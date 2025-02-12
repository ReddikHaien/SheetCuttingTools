using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using SheetCuttingTools.Abstractions.Contracts;
using SheetCuttingTools.Abstractions.Models;
using SheetCuttingTools.Grasshopper.Models;
using SheetCuttingTools.Grasshopper.Models.Internal;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace SheetCuttingTools.Grasshopper.Helpers
{
    internal static class GeometryConverter
    {
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
