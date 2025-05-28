using g3;
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
    public class SegmentDimensionConstraint(Vector3d dimensions) : ISegmentConstraint
    {
        private readonly Vector3d dimensions = dimensions;

        public string Name()
            => $"{ISegmentConstraint.RootName}/Dimension"; 

        public bool ValidateSegment(in SegmentCandidate candidate)
        {
            var seg = candidate.Segment;
            (Vector3d min, Vector3d max) = candidate.Segment.Polygons
                .SelectMany(p => p.Points).Select(x => seg.Vertices[x])
                .Aggregate(static (a, b) => a.Min(b), static (a, b) => a.Max(b));
            
            var dim = (max - min).Abs;
            return dim.x < dimensions.x && dim.y < dimensions.y && dim.z < dimensions.z;
        }
    }
}
