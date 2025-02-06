using SheetCuttingTools.Abstractions.Behaviors;
using SheetCuttingTools.Infrastructure.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Behaviors.SegmentConstraints
{
    /// <summary>
    /// Ensures that a segment is kept within a specified size.
    /// </summary>
    /// <param name="dimensions">The maximum size of the segments</param>
    public class SegmentDimensionConstraint(Vector3 dimensions) : ISegmentConstraint
    {
        private readonly Vector3 dimensions = dimensions;

        public string Name()
            => "Behavior/SegmentConstraint/SegmentDimensionConstraint"; 

        public bool ValidateSegment(in SegmentCandidate candidate)
        {
            var seg = candidate.Segment;
            (Vector3 min, Vector3 max) = candidate.Segment.Polygons
                .SelectMany(p => p.Points).Select(x => seg.Vertices[x])
                .Aggregate(Vector3.Min, Vector3.Max);
            
            var dim = Vector3.Abs(max - min);
            return dim.X < dimensions.X && dim.Y < dimensions.Y && dim.Z < dimensions.Z;
        }
    }
}
