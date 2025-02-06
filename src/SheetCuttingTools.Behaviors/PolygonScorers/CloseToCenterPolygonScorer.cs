using SheetCuttingTools.Abstractions.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Behaviors.PolygonScorers
{
    /// <summary>
    /// Scores a polygon based on how close it is to the segments center.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The center values are computed as the average of all the points in the polygon and segment respectively.
    /// </para>
    /// </remarks>
    public class CloseToCenterPolygonScorer : IPolygonScorer
    {
        public string Name()
            => "Behavior/PolygonScorer/CloseToCenterPolygonScorer";

        public float ScorePolygon(in PolygonScorerCandidate candidate)
        {
            var s = candidate.Segment;
            var p = candidate.Polygon.Points;
            var l = p.Length;

            var c = p.Select(x => s.Vertices[x]).Aggregate(Vector3.Add) / l;

            var d = Vector3.Distance(c, s.Center);

            return d == 0 ? float.MaxValue : 1.0f / d;
        }
    }
}
