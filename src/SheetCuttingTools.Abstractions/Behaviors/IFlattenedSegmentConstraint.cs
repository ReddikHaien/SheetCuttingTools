using SheetCuttingTools.Abstractions.Contracts;
using SheetCuttingTools.Abstractions.Models;
using SheetCuttingTools.Abstractions.Models.Numerics;
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
        /// <summary>
        /// The first anchor point for the <see cref="PlacedPolygon"/>.
        /// </summary>
        public HighPresVector2 AnchorA { get; init; }

        /// <summary>
        /// The second anchor point for the <see cref="PlacedPolygon"/>.
        /// </summary>
        public HighPresVector2 AnchorB { get; init; }

        /// <summary>
        /// The anchor edge for the <see cref="PlacedPolygon"/>.
        /// </summary>
        public Edge AnchorEdge { get; init; }

        /// <summary>
        /// Points belonging to the <see cref="PlacedPolygon"/>.
        /// </summary>
        public (int Index, HighPresVector2 Point)[] GeneratedPoints { get; init; }

        /// <summary>
        /// The polygon being placed.
        /// </summary>
        public Polygon PlacedPolygon { get; init; }

        /// <summary>
        /// Parent segment being flattened
        /// </summary>
        public IGeometryProvider Segment { get; init; }

        /// <summary>
        /// Points already flattened.
        /// </summary>
        public IReadOnlyList<HighPresVector2> FlattenedPoints { get; init; }

        /// <summary>
        /// The boundary of the flattened segment.
        /// </summary>
        public IEnumerable<(Edge Original, Edge Placed)> Boundary { get; init; }
        
    }
}
