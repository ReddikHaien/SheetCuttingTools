using SheetCuttingTools.Abstractions.Contracts;
using SheetCuttingTools.Abstractions.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Abstractions.Behaviors
{
    /// <summary>
    /// Applies a constraint to a <see cref="Segment"/>
    /// </summary>
    public interface ISegmentConstraint : IBehavior
    {
        /// <summary>
        /// Checks a given segment to see if it's valid for this constraint.
        /// </summary>
        /// <param name="candidate">The segment and surrounding context.</param>
        /// <returns><see langword="true"/> if thw segment is valid for this constraint.</returns>
        public bool ValidateSegment(in SegmentCandidate candidate);

    }

    public readonly struct SegmentCandidate(Guid id, IReadOnlyList<Polygon> polygons, IGeometry segment)
    {
        /// <summary>
        /// The segment id.
        /// </summary>
        public Guid Id { get; } = id;

        /// <summary>
        /// The segment polygons.
        /// </summary>
        public IReadOnlyList<Polygon> Polygons { get; } = polygons;

        /// <summary>
        /// The parent model.
        /// </summary>
        public IGeometry Segment { get; } = segment;
    }
}
