using g3;
using SheetCuttingTools.Abstractions.Contracts;
using SheetCuttingTools.Abstractions.Models;
using SheetCuttingTools.Abstractions.Models.Numerics;
using System.Numerics;

namespace SheetCuttingTools.Infrastructure.Math
{
    public static class GeometryMath
    {

        //    f f b b b f
        //    0  1  2  3  4  5
        // 0: 0  1  2  3  4  5
        // 1: 0  1  2  3  4  5
        // 2: 0  1  6  3  4  5
        // 3: 0  1  6  -1 4  5
        // 4: 0  1  6  -1 7  5
        // 5: 0  1  6  -1 7  5

        /// <summary>
        /// https://en.wikipedia.org/wiki/Sutherland%E2%80%93Hodgman_algorithm
        /// </summary>
        /// <param name="polygon"></param>
        /// <param name="geometry"></param>
        /// <param name="planes"></param>
        /// <returns></returns>
        public static (Polygon, Vector3d[]) CutPolygon(Polygon polygon, IGeometry geometry, params Plane3d[] planes)
        {
            List<int> points = [.. polygon.Points];
            List<Vector3d> added = [];

            foreach (var plane in planes)
            {
                List<int> l = [.. points];
                List<Vector3d> a = [.. added];
                points.Clear();
                added.Clear();

                for (int i = 0; i < l.Count; i++)
                {
                    int j = (i + l.Count - 1) % l.Count;

                    Vector3d prev = l[j] < 0 ? a[-l[j] - 1] : geometry.Vertices[l[j]];
                    Vector3d current = l[i] < 0 ? a[-l[i] - 1] : geometry.Vertices[l[i]];

                    GeometryMath.PointOnPlane(plane, current, prev, out var intersectionPoint);

                    bool currentRight = plane.WhichSide(current) >= 0;
                    bool prevRight = plane.WhichSide(prev) >= 0;

                    if (currentRight)
                    {
                        if (!prevRight)
                        {
                            added.Add(intersectionPoint);
                            points.Add(-added.Count);
                        }

                        if (l[i] < 0)
                        {
                            added.Add(current);
                            points.Add(-added.Count);
                        }
                        else
                        {
                            points.Add(l[i]);
                        }
                    }
                    else if (prevRight)
                    {
                        added.Add(intersectionPoint);
                        points.Add(-added.Count);

                    }
                }
            }

            int[] mapped = [.. points];

            return (new Polygon(mapped), [.. added]);
        }

        public static bool PointOnPlane(Plane3d plane3, Vector3d a, Vector3d b, out Vector3d pointOnPlane)
        {
            pointOnPlane = default;

            var u = b - a;
            var denom = plane3.Normal.Dot(u);
            if (denom == 0)
            {
                return false;
            }

            double t = (plane3.Constant - plane3.Normal.Dot(a)) / denom;
            pointOnPlane = a + u * t;
            return true;
        }

        public static Vector3d VectorToLine(Vector3d point, Vector3d p1, Vector3d p2)
        {
            var projected = p1 + (point - p1).Dot(p2 - p1) / p2.DistanceSquared(p1) * (p2 - p1);
            return projected - point;
        }

        public static Vector2d NormalToLine(Vector2d point, Vector2d p1, Vector2d p2)
        {
            var d = p2.Distance(p1);
            if (d == 0)
            {
                return point - p2;
            }

            var projected = p1 + (point - p1).Dot(p2 - p1) / p2.DistanceSquared(p1) * (p2 - p1);
            return (point - projected).Normalized;
        }

        public static Vector2 NormalToLine(Vector2 point, Vector2 p1, Vector2 p2)
        {
            var projected = p1 + Vector2.Dot(point - p1, p2 - p1) / Vector2.DistanceSquared(p2, p1) * (p2 - p1);
            return Vector2.Normalize(point - projected);
        }

        public static Vector2d LineIntersection(Vector2d a, Vector2d b, Vector2d c, Vector2d d)
        {
            var x1 = a.x;
            var x2 = b.x;
            var x3 = c.x;
            var x4 = d.x;

            var y1 = a.y;   
            var y2 = b.y;
            var y3 = c.y;
            var y4 = d.y;

            var x1y2 = x1 * b.y;
            var y1x2 = a.y * x2;
            var x3y4 = x3 * d.y;
            var y3x4 = c.y * x4;

            var div = (x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4);
            var px1 = (x1y2 - y1x2) * (x3 - x4) - (x1 - x2) * (x3y4 - y3x4);
            var py1 = (x1y2 - y1x2) * (y3 - y4) - (y1 - y2) * (x3y4 - y3x4);
            return new(px1 / div, py1 / div);
        }

        public static bool LineOverlap(Vector2d a, Vector2d b, Vector2d c, Vector2d d)
        {
            var x43 = d.x - c.x;
            var y43 = d.y - c.y;
            var x13 = a.x - c.x;
            var y13 = a.y - c.y;
            var x21 = b.x - a.x;
            var y21 = b.y - a.y;

            var ua = (x43 * y13 - y43 * x13) / (y43 * x21 - x43 * y21);
            var ub = (x21 * y13 - y21 * x13) / (y43 * x21 - x43 * y21);

            return (0.0001 < ua && ua < 0.999) && (0.0001 < ub && ub < 0.999);
        }

        public static bool LineOverlap(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            var x43 = d.X - c.X;
            var y43 = d.Y - c.Y;
            var x13 = a.X - c.X;
            var y13 = a.Y - c.Y;
            var x21 = b.X - a.X;
            var y21 = b.Y - a.Y;

            var ua = (x43 * y13 - y43 * x13) / (y43 * x21 - x43 * y21);
            var ub = (x21 * y13 - y21 * x13) / (y43 * x21 - x43 * y21);

            return (0.0001f < ua && ua < 0.999f) && (0.0001f < ub && ub < 0.999f);
        }

        public static Vector2d ComputeTrianglePoint(in Vector2d anchorA, in Vector2d anchorB, in Vector2d normal, in Vector3d triangleSides)
        {
            var ab = triangleSides.x;
            var bc = triangleSides.y;
            var ac = triangleSides.z;

            var diffx = anchorB.x - anchorA.x;
            var diffy = anchorB.y - anchorA.y;

            var l = (ac * ac - bc * bc + ab * ab) / (2 * ab);

            var q = ac * ac - l * l;

            if (q < 0)
                q = 0;

            var h = System.Math.Sqrt(q);

            var ld = l / ab;
            var hd = h / ab;

            var x1 = ld * diffx + hd * diffy + anchorA.x;
            var x2 = ld * diffx - hd * diffy + anchorA.x;

            var y1 = ld * diffy - hd * diffx + anchorA.y;
            var y2 = ld * diffy + hd * diffx + anchorA.y;

            var c1 = new Vector2d((double)x1, (double)y1);
            var c2 = new Vector2d((double)x2, (double)y2);

            //no need to check the normal if the points are the same.
            if (System.Math.Abs(x1 - x2) < double.Epsilon && System.Math.Abs(y1 - y2) < double.Epsilon)
            {
                if (ac > bc)
                {
                    var n = (anchorB - anchorA).Normalized;
                    var newp = (n * bc) + anchorB;
                    return newp;
                }
                else
                {
                    var n = (anchorA - anchorB).Normalized;
                    var newp = (n * ac) + anchorA;
                    return newp;
                }
            }

            var d1 = NormalToLine(c1, anchorA, anchorB).Dot(normal);
            var d2 = NormalToLine(c2, anchorA, anchorB).Dot(normal);

            if (d1 >= 0)
            {
                return c1;
            }
            else if (d2 >= 0)
            {
                return c2;
            }
            else
            {
                throw new InvalidOperationException("Failed to find a suitable point");
            }
        }

        /// <summary>
        /// Computes the point C so that the triangle A B C has the side lengths BC and AC
        /// </summary>
        /// <param name="anchorA"></param>
        /// <param name="anchorB"></param>
        /// <param name="normal"></param>
        /// <param name="triangleSides">The sides of the triangle, has the following mapping <c>X: ab</c>, <c>Y: bc</c>, <c>Z: ac</c></param>
        /// <exception cref="InvalidOperationException">Thrown if point c is uncomputable</exception>
        /// <returns></returns>
        public static Vector2 ComputeTrianglePoint(in Vector2 anchorA, in Vector2 anchorB, in Vector2 normal, in Vector3 triangleSides)
        {
            var ab = triangleSides.X;
            var bc = triangleSides.Y;
            var ac = triangleSides.Z;

            var diffx = anchorB.X - anchorA.X;
            var diffy = anchorB.Y - anchorA.Y;

            var l = (ac * ac - bc * bc + ab * ab) / (2 * ab);
            var h = System.Math.Sqrt(ac * ac - l * l);

            var ld = l / ab;
            var hd = h / ab;

            var x1 = ld * diffx + hd * diffy + anchorA.X;
            var x2 = ld * diffx - hd * diffy + anchorA.X;

            var y1 = ld * diffy - hd * diffx + anchorA.Y;
            var y2 = ld * diffy + hd * diffx + anchorA.Y;

            var c1 = new Vector2((float)x1, (float)y1);
            var c2 = new Vector2((float)x2, (float)y2);

            //no need to check the normal if the points are the same.
            if (System.Math.Abs(x1 - x2) < double.Epsilon && System.Math.Abs(y1 - y2) < double.Epsilon)
                return c1;

            if (Vector2.Dot(NormalToLine(c1, anchorA, anchorB), normal) >= 0)
            {
                return c1;
            }
            else if (Vector2.Dot(NormalToLine(c2, anchorA, anchorB), normal) >= 0)
            {
                return c2;
            }
            else
            {
                throw new InvalidOperationException("Failed to find a suitable point");
            }
        }
    }
}
