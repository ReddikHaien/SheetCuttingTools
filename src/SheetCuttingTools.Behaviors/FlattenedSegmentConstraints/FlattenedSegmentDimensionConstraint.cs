using g3;
using SheetCuttingTools.Abstractions.Behaviors;
using SheetCuttingTools.Abstractions.Models.Numerics;
using SheetCuttingTools.Infrastructure.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Behaviors.FlattenedSegmentConstraints
{
    public class FlattenedSegmentDimensionConstraint(double width, double height) : IFlattenedSegmentConstraint
    {
        public string Name()
            => "Behavior/FlattenedSegmentConstraint/DimensionConstraint";

        public bool ValidateFlatSegment(in FlattenedSegmentCandidate candidate)
        {
            (Vector2d min, Vector2d max) = candidate.FlattenedPoints
                .Concat(candidate.GeneratedPoints.Select(x => x.Point))
                .Aggregate(static (a, b) => a.Min(b), static (a, b) => a.Max(b));

            var dist = max - min;
            return dist.x < width && dist.y < height || dist.x < height && dist.y < width;
        }
    }
}
