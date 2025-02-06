using SheetCuttingTools.Abstractions.Behaviors;
using System.Numerics;

namespace SheetCuttingTools.Behaviors.EdgeFilters
{
    /// <summary>
    /// Filter based on edge length. Only edges with <paramref name="minEdgeLength"/> length pass.
    /// </summary>
    /// <param name="minEdgeLength">The minimum edge length.</param>
    public class MinEdgeLengthFilter(float minEdgeLength) : IEdgeFilter
    {
        private readonly float minEdgeLengthSquared = minEdgeLength * minEdgeLength;

        public bool FilterEdge(in EdgeFilterCandidate candidate)
        {
            (Vector3 a, Vector3 b) = candidate.Model.GetVertices(candidate.Edge);
            return Vector3.DistanceSquared(a, b) >= minEdgeLengthSquared;
        }

        public string Name()
            => "Behavior/EdgeFilter/MinEdgeLengthFilter";
    }
}
