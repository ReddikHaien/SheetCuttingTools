using SheetCuttingTools.Abstractions.Contracts;
using SheetCuttingTools.Abstractions.Models;

namespace SheetCuttingTools.Abstractions.Behaviors
{
    /// <summary>
    /// Returns a <see cref="float"/> representing a score for a given <see cref="Polygon"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When multiple polygon scores are used, the average value will be used as the score.
    /// </para>
    /// </remarks>
    public interface IPolygonScorer : IBehavior
    {
        /// <summary>
        /// Returns a score for a polygon.
        /// </summary>
        /// <param name="candidate">context for the polygon to be scored.</param>
        /// <returns>A <see cref="float"/>.</returns>
        float ScorePolygon(in PolygonScorerCandidate candidate);
    }

    /// <summary>
    /// Represents a polygon to be scored and surrounding context.
    /// </summary>
    /// <param name="polygon">The polygon to score.</param>
    /// <param name="model">The model.</param>
    public readonly struct PolygonScorerCandidate(Polygon polygon, IGeometryProvider model)
    {
        /// <summary>
        /// The polygon to be scored.
        /// </summary>
        public Polygon Polygon { get; } = polygon;

        /// <summary>
        /// The parent model.
        /// </summary>
        public IGeometryProvider Segment { get; } = model;
    }
}
