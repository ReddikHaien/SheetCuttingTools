using g3;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using SheetCuttingTools.Abstractions.Contracts;
using SheetCuttingTools.Abstractions.Models;
using SheetCuttingTools.Abstractions.Models.Geometry;
using SheetCuttingTools.Grasshopper.Models;
using SheetCuttingTools.Grasshopper.Models.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using static Rhino.FileIO.FileObjWriteOptions;

namespace SheetCuttingTools.Grasshopper.Helpers
{
    internal static class GeometryConverter
    {
        public static IGeometry CreateGeometry(this object value)
            => value switch
            {
                GH_ObjectWrapper b => b.Value.CreateGeometry(),
                
                GH_Mesh m => m.Value.CreateGeometry(),
                GH_SubD s => s.Value.CreateGeometry(),
                GH_Brep b => b.Value.CreateGeometry(),
                
                Mesh m => m.CreateGeometry(),
                SubD s => s.CreateGeometry(),
                Brep b => b.CreateGeometry(),
                
                IGeometry i => i,
                GH_Geometry i => i.Value,

                _ => throw new NotImplementedException("missing values")
            };

        private static IGeometry CreateGeometry(this SubD subD)
        {
            subD = (SubD)subD.Duplicate();
            subD.Subdivide(1);
            var boundingBox = subD.GetBoundingBox(true);
            OctTree tree = new(boundingBox.Min.ToG3Vector3d(), boundingBox.Max.ToG3Vector3d(), 0.01);

            List<g3.Vector3d> verts = [];
            List<g3.Vector3f> normals = [];
            List<Polygon> polygons = [];

            foreach(var face in subD.Faces)
            {
                int count = face.VertexCount;
                int[] polygon = new int[count];

                for(int i = 0; i < count; i++)
                {
                    var vertex = face.VertexAt(i);
                    var p = vertex.SurfacePoint().ToG3Vector3d();
                    if (!tree.GetValue(p, out var index))
                    {
                        verts.Add(p);
                        normals.Add(face.SurfaceCenterNormal.ToG3Vector3f());
                        index = verts.Count - 1;
                        tree.AddPoint(p, index);
                    }

                    polygon[i] = index;
                }
                polygons.Add(new Polygon(polygon));
            }

            return new RawGeometry
            {
                Vertices = verts.ToArray().AsReadOnly(),
                Normals = normals.ToArray().AsReadOnly(),
                Polygons = polygons.ToArray().AsReadOnly(),
                Center3d = verts.Aggregate((a, b) => a + b) / verts.Count
            };
        }

        private static IGeometry CreateGeometry(this Brep brep)
        {
            var all = Mesh.CreateFromBrep(brep, MeshingParameters.QualityRenderMesh);

            var merged = new Mesh();

            foreach(var m in all)
            {
                merged.Append(m);
            }

            return merged.CreateGeometry();
        }

        private static IGeometry CreateGeometry(this Mesh mesh)
        {

            var boundingBox = mesh.GetBoundingBox(true);

            OctTree octree = new(boundingBox.Min.ToG3Vector3d(), boundingBox.Max.ToG3Vector3d(), 0.001);

            List<g3.Vector3d> verts = [];
            List<g3.Vector3f> normals = [];
            List<int> mergedPoints = [];
            List<Polygon> polygons = [];
            Dictionary<uint, int> mapped = [];

            mesh.MergeAllCoplanarFaces(0.0);
            
            foreach(var bound in mesh.GetNgons())
            {
                int[] points = new int[bound.Length];
                int i = 0;

                foreach (uint idx in bound)
                {
                    if (!mapped.TryGetValue(idx, out var map))
                    {
                        var p = mesh.Vertices[(int)idx].ToG3Vector3d();

                        if (!octree.GetValue(p, out var identical))
                        {
                            map = verts.Count;
                            verts.Add(p);
                            octree.AddPoint(p, map);
                            normals.Add(mesh.Normals[(int)idx].ToG3Vector3f());
                            mergedPoints.Add(1);
                        }
                        else
                        {
                            map = identical;
                            var merged = mergedPoints[map];
                            var normal = normals[map] * merged;
                            normals[map] = (normal + mesh.Normals[(int)idx].ToG3Vector3f()) / (merged + 1);
                            mergedPoints[map]++;
                        }

                        mapped.Add(idx, map);
                    }

                    points[i++] = map;
                }

                polygons.Add(new(points));
            }

            return new RawGeometry
            {
                Normals = normals.ToArray().AsReadOnly(),
                Vertices = verts.ToArray().AsReadOnly(),
                Polygons = polygons,
                Parent = null!
            };
        }

        private static IEnumerable<int[]> GetNgons(this Mesh mesh)
        {

            foreach (var ngon in mesh.GetNgonAndFacesEnumerable())
            {
                var edges = ngon.FaceIndexList().Select(x => mesh.Faces[(int)x]).SelectMany<MeshFace, Edge>(x => {
                    if (x.IsQuad)
                    {
                        return [new Edge(x.A, x.B), new(x.B, x.C), new(x.C, x.D), new(x.D, x.A)];
                    }
                    else
                    {
                        return [new Edge(x.A, x.B), new(x.B, x.C), new(x.C, x.A)];
                    }
                }).ToLookup(x => x);

                var boundEdges = edges.Where(x => x.Count() < 2).Select(x => x.Key).ToList();

                List<int[]> bounds = [];
                while(boundEdges.Count > 0)
                {
                    List<int> curBound = [];
                    bool found = true;
                    while (found)
                    {
                        found = false;
                        for(int i = 0; i < boundEdges.Count; i++)
                        {
                            var edge = boundEdges[i];

                            if (curBound.Count == 0)
                            {
                                curBound.Add(edge.A);
                                curBound.Add(edge.B);
                            }
                            else if (curBound[^1] == edge.A)
                            {
                                curBound.Add(edge.B);
                            }
                            else if (curBound[^1] == edge.B)
                            {
                                curBound.Add(edge.A);
                            }
                            else if (curBound[0] == edge.A)
                            {
                                curBound.Insert(0, edge.B);
                            }
                            else if (curBound[0] == edge.B)
                            {
                                curBound.Insert(0, edge.A);
                            }
                            else
                            {
                                continue;
                            }

                            found = true;
                            boundEdges.RemoveAt(i--);
                        }
                    }
                    if (curBound[0] == curBound[^1])
                    {
                        curBound.RemoveAt(curBound.Count - 1);
                    }
                    bounds.Add([.. curBound]);
                }

                if (bounds.Count == 1)
                {
                    yield return bounds[0];
                }
                else
                {
                    //TODO: find a better way to create perfect ngons without holes.
                    foreach (var faceIndex in ngon.FaceIndexList())
                    {
                        var face = mesh.Faces[(int)faceIndex];
                        int[] indices = face.IsQuad
                            ? [face.A, face.B, face.C, face.D]
                            : [face.A, face.B, face.C];

                        yield return indices;
                    }
                }
            }
        }

        

        public static IGeometryProvider CreateGeometryProvider(this object value)
            => value switch
            {
                GH_ObjectWrapper o => o.Value.CreateGeometryProvider(),
                IGeometryProvider gp => gp,
                GH_Segment s => s.Value,
                Mesh m => m.CreateGeometryProvider(),
                GH_Mesh m => m.Value.CreateGeometryProvider(),
                Brep b => new BrepSegment(b),
                GH_Brep b => new BrepSegment(b.Value),
                _ => throw new InvalidOperationException($"{value.GetType().Name} is not a supported geometry type")
            };

        private static IGeometryProvider CreateGeometryProvider(this Mesh mesh)
        {
            mesh.MergeAllCoplanarFaces(0.0);

            List<Vector3> verts = [];
            List<Vector3> normals = [];
            List<int> mergedPoints = [];
            List<Polygon> polygons = [];
            Dictionary<uint, int> mapped = [];

            foreach (var ngon in mesh.GetNgonAndFacesEnumerable())
            {
                int[] points = new int[ngon.BoundaryVertexCount];
                int i = 0;
                foreach (uint idx in ngon.BoundaryVertexIndexList())
                {
                    if (!mapped.TryGetValue(idx, out var map))
                    {
                        var p = mesh.Vertices[(int)idx].ToVector3();
                        var identical = verts.FindIndex(x => Vector3.Distance(x, p) < 0.0001f);

                        if (identical == -1)
                        {
                            map = verts.Count;
                            verts.Add(p);
                            normals.Add(mesh.Normals[(int)idx].ToVector3());
                            mergedPoints.Add(1);
                        }
                        else
                        {
                            map = identical;
                            var merged = mergedPoints[map];
                            var normal = normals[map] * merged;
                            normals[map] = (normal + mesh.Normals[(int)idx].ToVector3()) / (merged + 1);
                            mergedPoints[map]++;
                        }

                        mapped.Add(idx, map);
                    }

                    points[i++] = map;
                }

                polygons.Add(new(points));
            }

            return new Model([.. verts], [.. normals], [.. polygons]);
        }
    }
}
