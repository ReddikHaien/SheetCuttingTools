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
                GH_SegmentV2 i => i.Value,

                _ => throw new NotImplementedException("missing value")
            };

        private static IGeometry CreateGeometry(this SubD subD)
            => Mesh.CreateFromSubD(subD, 1).CreateGeometry();
        
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
            var builder = new DMesh3Builder();

            List<g3.Vector3d> verts = [];
            List<g3.Vector3f> normals = [];
            List<int> mergedPoints = [];
            List<Polygon> polygons = [];
            Dictionary<uint, int> mapped = [];

            mesh.MergeAllCoplanarFaces(0.0);
            
            foreach(var bound in mesh.GetNgonAndFacesEnumerable())
            {
                int[] points = new int[bound.BoundaryVertexCount];
                int i = 0;

                foreach (uint idx in bound.BoundaryVertexIndexList())
                {
                    if (!mapped.TryGetValue(idx, out var map))
                    {
                        var p = mesh.Vertices[(int)idx].ToG3Vector3d();
                        var identical = verts.FindIndex(x => x.EpsilonEqual(p, 0.0001));

                        if (identical == -1)
                        {
                            map = verts.Count;
                            verts.Add(p);
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

                    if (bounds.Count == 2)
                    {
                        // lists
                        // 1, 2, 3, 4, 5
                        // a, b, c, d, e
                        // merged
                        // 1, 2, 3, 4, 5, a, b, c, d, e

                        var a = bounds[0][0];
                        var pa = mesh.Vertices[a];

                        int b = -1;
                        float dist = float.MaxValue;
                        foreach(var x in bounds[1])
                        {
                            var px = mesh.Vertices[b];
                            var d = (pa - px).SquareLength;
                            
                            if (d > dist)
                            {
                                b = x;
                                dist = d;
                            }
                        }

                        var pb = mesh.Vertices[b];


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
