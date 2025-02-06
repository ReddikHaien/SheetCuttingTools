using SheetCuttingTools.Abstractions.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Abstractions.Contracts
{
    /// <summary>
    /// Base interface for classes that provide geometry information.
    /// </summary>
    public interface IGeometryProvider
    {
        /// <summary>
        /// The polygons in this geometry.
        /// </summary>
        public IReadOnlyList<Polygon> Polygons { get; }

        /// <summary>
        /// The points in this geometry.
        /// </summary>
        public IReadOnlyList<Vector3> Vertices { get; }

        /// <summary>
        /// The normals in this geometry.
        /// </summary>
        public IReadOnlyList<Vector3> Normals { get; }

        /// <summary>
        /// Returns the vertices for an edge.
        /// </summary>
        /// <param name="edge">The edge to get vertices for.</param>
        /// <returns>A <see cref="ValueTuple{T1, T2}"/> of <see cref="Vector3"/>. </returns>
        (Vector3 A, Vector3 B) GetVertices(Edge edge);

        /// <summary>
        /// Returns the normals for a polygon.
        /// </summary>
        /// <param name="polygon">The polygon to get normals for.</param>
        /// <returns>An array of <see cref="Vector3"/>.</returns>
        Vector3[] GetNormals(Polygon polygon);

        /// <summary>
        /// Returns the points for a polygon.
        /// </summary>
        /// <param name="polygon">The polygon to get points for.</param>
        /// <returns>An array of <see cref="Vector3"/>.</returns>
        Vector3[] GetVertices(Polygon polygon);

        public Vector3 Center { get; set; }
    }
}
