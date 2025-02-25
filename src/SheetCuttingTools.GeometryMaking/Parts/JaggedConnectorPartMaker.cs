using g3;
using SheetCuttingTools.Abstractions.Contracts;
using SheetCuttingTools.Abstractions.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.GeometryMaking.Parts
{
    public class JaggedConnectorPartMaker(int numTags, double gapSize, double tagHeight) : IPartMaker
    {
        private readonly int numTags = numTags;
        private readonly double gapSize = gapSize;
        private readonly double tagHeight = tagHeight;

        public void CreatePart(Edge edge, Vector2d pointA, Vector2d pointB, Vector2d normal, IFlattenedGeometry flattenedGeometry, PartMakerContext context)
        {
            var pointOffsets = pointA.Distance(pointB) / numTags;

            var side = context.MaleSide ? 1 : -1;

            List<Vector2d> vertices = [];

            Vector2d ab = (pointB - pointA).Normalized;

            for (int i = 0; i < numTags*2 - 1;)
            {
                vertices.Add(pointA + ab * pointOffsets * (i+1) + normal * tagHeight * side);
            }

            context.AddLine([pointA, .. vertices, pointB]);
        }

        public double GetRequiredGap(bool maleSide)
            => gapSize;
    }
}
