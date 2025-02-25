using g3;
using SheetCuttingTools.Abstractions.Contracts;
using SheetCuttingTools.Abstractions.Models;
using SheetCuttingTools.Infrastructure.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.GeometryMaking.Parts
{
    public class LatticeHingePartMaker(double gapSize)
    {
        private readonly double gapSize = gapSize;

        private readonly double hingeWidthPercentage = 0.1;

        private readonly double rodWidth = 0.5;

        public double GetRequiredGap(bool maleSide)
            => gapSize;

        public void CreatePart(Edge edge, Vector2d pointA, Vector2d pointB, Vector2d normal, IFlattenedGeometry flattenedGeometry, List<Vector2d> points, List<Edge> edges)
        {

            // ca                          cb
            // |                           | rw
            // z---------------------w     |
            //                       |     | gs - rw
            // pa ------------------ q     pb

            var l = pointA.Distance(pointB);
            var ab = (pointB - pointA).Normalized;

            var q = pointA + ab * l * (1 - hingeWidthPercentage);
            var w = q + normal * (gapSize - rodWidth);

            var ca = pointA + normal * gapSize;
            var z = ca - normal * rodWidth;

            var cb = pointB + normal * gapSize;

            points.AddRange([pointA, q]);
            edges.Add(new(points.Count - 2, points.Count - 1));

            points.AddRange([q, w]);
            edges.Add(new(points.Count - 2, points.Count - 1));

            points.AddRange([ca, z]);
            edges.Add(new(points.Count - 2, points.Count - 1));

            points.AddRange([w, z]);
            edges.Add(new(points.Count - 2, points.Count - 1));


            points.AddRange([pointB, cb]);
            edges.Add(new(points.Count - 2, points.Count - 1));
        }
    }
}
