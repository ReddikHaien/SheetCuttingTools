using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Infrastructure.Math
{
    public static class GeometryMath
    {
        public static Vector2 NormalToLine(Vector2 point, Vector2 p1, Vector2 p2)
        {
            var projected = p1 + (Vector2.Dot(point - p1, p2 - p1) / Vector2.DistanceSquared(p2, p1)) * (p2 - p1);

            return Vector2.Normalize(point - projected);
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
