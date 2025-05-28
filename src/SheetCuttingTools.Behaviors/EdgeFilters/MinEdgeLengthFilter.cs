using g3;
using SheetCuttingTools.Abstractions.Behaviors;
using SheetCuttingTools.Infrastructure.Extensions;
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
            (Vector3d a, Vector3d b) = candidate.Model.GetVertices(candidate.Edge);
            return a.DistanceSquared(b) >= minEdgeLengthSquared;
        }

        public string Name()
            => $"{IEdgeFilter.RootName}/MinEdgeLength";
    }
}
