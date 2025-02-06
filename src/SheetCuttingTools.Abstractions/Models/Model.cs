using SheetCuttingTools.Abstractions.Contracts;
using System.Numerics;

namespace SheetCuttingTools.Abstractions.Models
{
    /// <summary>
    /// Represents a model that can be segmented.
    /// </summary>
    public class Model : IGeometryProvider
    {
        public Vector3[] Vertices { get; }
        public Vector3[] Normals { get; }
        public Polygon[] Polygons { get; }

        IReadOnlyList<Polygon> IGeometryProvider.Polygons => Polygons;

        IReadOnlyList<Vector3> IGeometryProvider.Vertices => Vertices;

        IReadOnlyList<Vector3> IGeometryProvider.Normals => Normals;

        public Vector3 Center { get; set; }

        /// <param name="vertices">The vertices of the model.</param>
        /// <param name="normals">The normals of the model.</param>
        /// <param name="polygons">The polygons of the model.</param>
        public Model(Vector3[] vertices, Vector3[] normals, Polygon[] polygons)
        {
            Vertices = vertices;
            Normals = normals;
            Polygons = polygons;
            Center = vertices.Aggregate(Vector3.Add) / vertices.Length;
        }

        public (Vector3 A, Vector3 B) GetVertices(Edge edge)
            => (Vertices[edge.A], Vertices[edge.B]);

        public Vector3[] GetVertices(Polygon polygon)
        {
            var l = polygon.Points.Length;
            var arr = new Vector3[l];
            for (int i = 0;i < l; i++)
            {
                arr[i] = Vertices[polygon.Points[i]];
            }
            return arr;
        }

        public Vector3[] GetNormals(Polygon polygon)
        {
            var l = polygon.Points.Length;
            var arr = new Vector3[l];
            for (int i = 0; i < l; i++)
            {
                arr[i] = Normals[polygon.Points[i]];
            }
            return arr;
        }
    }
}
