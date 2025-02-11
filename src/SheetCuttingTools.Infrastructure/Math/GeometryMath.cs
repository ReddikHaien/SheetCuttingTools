using SheetCuttingTools.Abstractions.Models.Numerics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Infrastructure.Math
{
    public static class GeometryMath
    {
        public static HighPresVector2 NormalToLine(HighPresVector2 point, HighPresVector2 p1, HighPresVector2 p2)
        {
            var projected = p1 + (HighPresVector2.Dot(point - p1, p2 - p1) / HighPresVector2.DistanceSquared(p2, p1)) * (p2 - p1);
            return HighPresVector2.Normalize(point - projected);
        }

        public static Vector2 NormalToLine(Vector2 point, Vector2 p1, Vector2 p2)
        {
            var projected = p1 + (Vector2.Dot(point - p1, p2 - p1) / Vector2.DistanceSquared(p2, p1)) * (p2 - p1);
            return Vector2.Normalize(point - projected);
        }

        public static bool LineOverlap(HighPresVector2 a, HighPresVector2 b, HighPresVector2 c, HighPresVector2 d)
        {
            var x43 = d.X - c.X;
            var y43 = d.Y - c.Y;
            var x13 = a.X - c.X;
            var y13 = a.Y - c.Y;
            var x21 = b.X - a.X;
            var y21 = b.Y - a.Y;

            var ua = (x43 * y13 - y43 * x13) / (y43 * x21 - x43 * y21);
            var ub = (x21 * y13 - y21 * x13) / (y43 * x21 - x43 * y21);

            return (0.0001 < ua && ua < 0.999) && (0.0001 < ub && ub < 0.999);
        }

        public static HighPresVector2 ComputeTrianglePoint(in HighPresVector2 anchorA, in HighPresVector2 anchorB, in HighPresVector2 normal, in HighPresVector3 triangleSides)
        {
            var ab = triangleSides.X;
            var bc = triangleSides.Y;
            var ac = triangleSides.Z;

            var diffx = anchorB.X - anchorA.X;
            var diffy = anchorB.Y - anchorA.Y;

            var l = (ac * ac - bc * bc + ab * ab) / (2 * ab);

            var q = ac * ac - l * l;

            if (q < 0 && q > -0.01)
                q = 0;

            var h = System.Math.Sqrt(q);

            var ld = l / ab;
            var hd = h / ab;

            var x1 = ld * diffx + hd * diffy + anchorA.X;
            var x2 = ld * diffx - hd * diffy + anchorA.X;

            var y1 = ld * diffy - hd * diffx + anchorA.Y;
            var y2 = ld * diffy + hd * diffx + anchorA.Y;

            var c1 = new HighPresVector2((double)x1, (double)y1);
            var c2 = new HighPresVector2((double)x2, (double)y2);

            //no need to check the normal if the points are the same.
            if (System.Math.Abs(x1 - x2) < double.Epsilon && System.Math.Abs(y1 - y2) < double.Epsilon)
            {
                if (ac > bc)
                {
                    var n = HighPresVector2.Normalize(anchorB - anchorA);
                    var newp = (n * bc) + anchorB;
                    return newp;
                }
                else
                {
                    var n = HighPresVector2.Normalize(anchorA - anchorB);
                    var newp = (n * ac) + anchorA;
                    return newp;
                }
            }

            if (HighPresVector2.Dot(NormalToLine(c1, anchorA, anchorB), normal) >= 0)
            {
                return c1;
            }
            else if (HighPresVector2.Dot(NormalToLine(c2, anchorA, anchorB), normal) >= 0)
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
            var h = System.Math.Sqrt(ac*ac - l*l);

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
