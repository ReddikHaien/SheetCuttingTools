using SheetCuttingTools.Abstractions.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Abstractions.Behaviors
{
    /// <summary>
    /// Interface for constraints applied to flattened segments. These differ from <see cref="ISegmentConstraint"/> since they are applied to the flattened version of the segment.
    /// </summary>
    public interface IFlattenedSegmentConstraint : IBehavior
    {
        /// <summary>
        /// Checks a given flattened segment to see if it's valid for this constraint.
        /// </summary>
        /// <param name="candidate">The segment and surrounding context.</param>
        /// <returns><see langword="true"/> if the segment is valid for this constraint.</returns>
        public bool ValidateFlatSegment(in FlattenedSegmentCandidate candidate);
    }

    public readonly struct FlattenedSegmentCandidate
    {
        public Vector2 AnchorA { get; init; }

        public Vector2 AnchorB { get; init; }

        public Edge AnchorEdge { get; init; }

        public (int, Vector2)[] GeneratedPoints { get; init; }

        public Polygon PlacedPolygon { get; init; }

        public Segment Segment { get; init; }
    }
}
