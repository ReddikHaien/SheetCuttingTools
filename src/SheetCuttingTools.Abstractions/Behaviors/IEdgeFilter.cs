using SheetCuttingTools.Abstractions.Contracts;
using SheetCuttingTools.Abstractions.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Abstractions.Behaviors
{
    public interface IEdgeFilter : IBehavior
    {
        /// <summary>
        /// returns a boolean value, that will be used to filter the provided edge.
        /// </summary>
        /// <param name="candidate">The edge to applie the filter to, and surrounding context.</param>
        /// <returns><see langword="true"/> if the edge is valid.</returns>
        public bool FilterEdge(in EdgeFilterCandidate candidate);
    }

    /// <summary>
    /// Context surrounding an edge that should be filtered.
    /// </summary>
    /// <param name="edge">The edge to test.</param>
    /// <param name="model">The parent model.</param>
    public readonly struct EdgeFilterCandidate(Edge edge, IGeometryProvider model)
    {
        /// <summary>
        /// The edge to test.
        /// </summary>
        public Edge Edge { get; } = edge;

        /// <summary>
        /// The parent model.
        /// </summary>
        public IGeometryProvider Model { get; } = model;
    }
}
