using Rhino.Geometry;
using System.Collections.Generic;

namespace SheetCuttingTools.Grasshopper.Helpers
{
    internal static class MeshNgonExtensions
    {
        public static IEnumerable<MeshFace> CreateMeshFaces(int[] indices, IReadOnlyList<Point3f> verts)
        {
            switch (indices.Length)
            {
                case 3:
                    yield return new MeshFace(indices[0], indices[1], indices[2]);
                    break;

                case 4:
                    yield return new MeshFace(indices[0], indices[1], indices[2], indices[3]);
                    break;

                default:
                    List<int> points = [.. indices];
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
                                yield return new MeshFace(points[ia], points[ib], points[ic]);
                                points.RemoveAt(ib);
                                break;
                            }
                        }
                    }

                    yield return new MeshFace(points[0], points[1], points[2]);

                    break;
            }
        }
    }
}
