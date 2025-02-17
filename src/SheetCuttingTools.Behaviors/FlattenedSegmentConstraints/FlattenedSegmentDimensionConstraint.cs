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
            (HighPresVector2 min, HighPresVector2 max) = candidate.FlattenedPoints
                .Concat(candidate.GeneratedPoints.Select(x => x.Point))
                .Aggregate(HighPresVector2.Min, HighPresVector2.Max);

            var dist = max - min;
            return dist.X < width && dist.Y < height;
        }
    }
}
