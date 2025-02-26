using g3;
using SheetCuttingTools.Abstractions.Contracts;
using SheetCuttingTools.Abstractions.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Infrastructure.Extensions
{
    public static class IGeometryExtensions
    {

        public static Vector2d[] GetPoints(this IFlattenedGeometry geometry, Polygon polygon, Vector2d[] buf = null!)
        {
            if (buf is null || buf.Length < polygon.Points.Length)
            {
                buf = new Vector2d[polygon.Points.Length];
            }

            for(int i = 0; i < polygon.Points.Length; i++)
            {
                buf[i] = geometry.Points[polygon.Points[i]];
            }
            return buf;
        }
        public static (Vector2d A, Vector2d) GetPoints(this IFlattenedGeometry geometry, Edge edge)
            => (geometry.Points[edge.A], geometry.Points[edge.B]);

        public static (Vector3d A, Vector3d B) GetVertices(this IGeometry geometry, Edge edge)
        {
            return (geometry.Vertices[edge.A], geometry.Vertices[edge.B]);
        }

        public static Vector3d[] GetVertices(this IGeometry geometry, Polygon polygon)
        {
            var l = polygon.Points.Length;
            var arr = new Vector3d[l];
            for (int i = 0; i < l; i++)
            {
                arr[i] = geometry.Vertices[polygon.Points[i]];
            }
            return arr;
        }

        public static Vector2d GetMidPoint2d(this IFlattenedGeometry geometry, Edge edge)
        {
            (Vector2d a, Vector2d b) = geometry.GetPoints(edge);
            return ( a + b) / 2;
        }

        public static Vector2d GetMidPoint2d(this IFlattenedGeometry geometry, Polygon polygon)
        {
            var l = polygon.Points.Length;
            return polygon.Points.Select(x => geometry.Points[x]).Aggregate((a, b) => a + b) / l;
        }

        public static Vector3d GetMidPoint3d(this IGeometry geometry, Edge edge)
        {
            (Vector3d a, Vector3d b) = geometry.GetVertices(edge);
            return (a + b) / 2;
        }

        public static Vector3d GetMidPoint3d(this IGeometry geometry, Polygon polygon)
        {
            var l = polygon.Points.Length;
            return polygon.Points.Select(i => geometry.Vertices[i]).Aggregate((a, b) => a + b) / l;
        }

        public static DMesh3 ConvertToDMesh3(this IGeometry geometry)
        {
            if (geometry is IHaveMesh meshGeometry)
                return meshGeometry.Mesh;

            Dictionary<int, int> mapping = [];

            var dmesh = new DMesh3();

            foreach(var triangle in geometry.Polygons.SelectMany(geometry.Triangulate))
            {
                if(!mapping.TryGetValue(triangle.a, out var a))
                {
                    a = dmesh.AppendVertex(geometry.Vertices[triangle.a]);
                    mapping[triangle.a] = a;
                }

                if (!mapping.TryGetValue(triangle.b, out var b))
                {
                    b = dmesh.AppendVertex(geometry.Vertices[triangle.b]);
                    mapping[triangle.b] = b;
                }

                if (!mapping.TryGetValue(triangle.c, out var c))
                {
                    c = dmesh.AppendVertex(geometry.Vertices[triangle.c]);
                    mapping[triangle.c] = c;
                }

                dmesh.AppendTriangle(a, b, c);
            }


            return dmesh;
        }

        public static Index3i[] Triangulate(this IGeometry geometry, Polygon polygon)
        {
            List<Index3i> triangles = [];
            List<int> points = [.. polygon.Points];
            while (points.Count > 3)
            {
                int l = points.Count;
                for (int ia = 0; ia < l; ia++)
                {
                    int ib = (ia + 1) % l;
                    int ic = (ia + 2) % l;

                    var triangle = new Triangle3d(
                        geometry.Vertices[points[ia]],
                        geometry.Vertices[points[ib]],
                        geometry.Vertices[points[ic]]
                    );

                    bool failed = false;

                    for (int k = 2, o = (ic + 1) % l; k < l; k++, o = (o + 1) % l)
                    {
                        var p = geometry.Vertices[points[o]];
                        var bary = triangle.BarycentricCoords(p);

                        if (bary.x > 0 && bary.y > 0 && bary.z > 0)
                        {
                            failed = true;
                            break;
                        }
                    }

                    if (!failed)
                    {
                        triangles.Add(new(points[ia], points[ib], points[ic]));
                        points.RemoveAt(ib);
                        break;
                    }
                }
            }

            triangles.Add(new(points[0], points[1], points[2]));

            return [.. triangles];
        }

        public static OctTree ConstructOctree(this IGeometry geometry, double epsilon = 0.01)
        {
            var (min, max) = geometry.Vertices.Aggregate((a, b) => a.Min(b), (a, b) => a.Max(b));

            var octTree = new OctTree(min, max, epsilon);

            foreach(var vert in geometry.Vertices)
            {
                octTree.AddPoint(vert, 0);
            }

            foreach(var edge in geometry.Polygons.SelectMany(x => x.GetEdges()).Distinct())
            {
                var (a, b) = geometry.GetVertices(edge);
                octTree.AddPoint((a + b) / 2, 0);
            }

            foreach(var poly in geometry.Polygons)
            {
                var p = geometry.GetVertices(poly).Aggregate(static (a, b) => a + b) / poly.Points.Length;
                octTree.AddPoint(p, 0);
            }

            Debug.WriteLine($"Created oct tree with {octTree.Count} points");

            return octTree;
        }
    }
}
