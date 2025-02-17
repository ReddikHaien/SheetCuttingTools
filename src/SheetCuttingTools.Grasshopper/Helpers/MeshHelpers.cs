using Rhino.Geometry;
using SheetCuttingTools.Abstractions.Contracts;
using SheetCuttingTools.Abstractions.Models;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace SheetCuttingTools.Grasshopper.Helpers
{
    internal static class MeshHelpers
    {
        public static Mesh CreateRhinoMesh(this IGeometry geometry)
        {
            var mesh = new Mesh();

            List<Point3f> verts = [];
            List<Vector3f> normals = [];
            List<Color> colors = [];
            List<MeshFace> faces = [];

            Dictionary<int, int> mapped = [];

            foreach (var polygon in geometry.Polygons)
            {
                int[] i = polygon.Points.Select(x =>
                {
                    if (!mapped.TryGetValue(x, out int j))
                    {
                        verts.Add(geometry.Vertices[x].ToPoint3f());
                        normals.Add(geometry.Normals[x].ToVector3f());
                        return verts.Count - 1;
                    }
                    return j;
                }).ToArray();

                colors.AddRange(i.Select(_ => ColorHelper.GetColor(geometry.GetHashCode())));

                switch (i.Length)
                {
                    case 3:
                        faces.Add(new(i[0], i[1], i[2]));
                        continue;

                    case 4:
                        faces.Add(new(i[0], i[1], i[2], i[3]));
                        break;

                    default:
                        List<int> points = [.. i];
                        while (points.Count > 3)
                        {
                            int l = points.Count;
                            for (int ia = 0; ia < l; ia++)
                            {
                                int ib = (ia + 1) % l;
                                int ic = (ia + 2) % l;

                                var triangle = new Triangle3d(
                                    verts[points[ia]],
                                    verts[points[ib]],
                                    verts[points[ic]]
                                );

                                bool failed = false;

                                for (int k = 2, o = (ic + 1) % l; k < l; k++, o = (o + 1) % l)
                                {
                                    var p = verts[points[o]];
                                    var bary = triangle.BarycentricCoordsAt(p, out var _);

                                    if (bary.X > 0 && bary.Y > 0)
                                    {
                                        failed = true;
                                        break;
                                    }
                                }

                                if (!failed)
                                {
                                    faces.Add(new(points[ia], points[ib], points[ic]));
                                    points.RemoveAt(ib);
                                    break;
                                }
                            }
                        }

                        faces.Add(new(points[0], points[1], points[2]));

                        break;
                }
            }

            mesh.Vertices.AddVertices(verts);
            mesh.Faces.AddFaces(faces);
            colors.ForEach(x => mesh.VertexColors.Add(x));
            normals.ForEach(x => mesh.Normals.Add(x));

            return mesh;
        }
    }
}
