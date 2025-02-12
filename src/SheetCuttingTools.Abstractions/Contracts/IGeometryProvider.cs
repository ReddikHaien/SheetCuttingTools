using SheetCuttingTools.Abstractions.Models;
using System.Numerics;

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
        (Vector3 A, Vector3 B) GetVertices(Edge edge)
            => (Vertices[edge.A], Vertices[edge.B]);

        /// <summary>
        /// Returns the normals for a polygon.
        /// </summary>
        /// <param name="polygon">The polygon to get normals for.</param>
        /// <returns>An array of <see cref="Vector3"/>.</returns>
        Vector3[] GetNormals(Polygon polygon)
        {
            var l = polygon.Points.Length;
            var arr = new Vector3[l];
            for (int i = 0; i < l; i++)
            {
                arr[i] = Normals[polygon.Points[i]];
            }
            return arr;
        }

        /// <summary>
        /// Returns the points for a polygon.
        /// </summary>
        /// <param name="polygon">The polygon to get points for.</param>
        /// <returns>An array of <see cref="Vector3"/>.</returns>
        Vector3[] GetVertices(Polygon polygon)
        {
            var l = polygon.Points.Length;
            var arr = new Vector3[l];
            for (int i = 0; i < l; i++)
            {
                arr[i] = Vertices[polygon.Points[i]];
            }
            return arr;
        }

        public Vector3 Center { get; set; }
    }
}
