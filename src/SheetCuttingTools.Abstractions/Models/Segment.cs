using SheetCuttingTools.Abstractions.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Abstractions.Models
{
    public class Segment(IGeometryProvider model) : IGeometryProvider
    {
        /// <summary>
        /// The input model used to make this segment.
        /// </summary>
        public IGeometryProvider Model { get; } = model;

        /// <summary>
        /// The unique identifier for this segment.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The parent for this segment, <see langword="null"/> if none.
        /// </summary>
        public Segment? Parent { get; set; }

        /// <summary>
        /// The polygons in this segment.
        /// </summary>
        public Polygon[] Polygons { get; set; } = [];

        public Vector3 Center { get; set; }

        /// <summary>
        /// Extension data. 
        /// </summary>
        public Dictionary<string, object> Extensions { get; set; } = [];

        IReadOnlyList<Polygon> IGeometryProvider.Polygons => Polygons;

        public IReadOnlyList<Vector3> Vertices => Model.Vertices;

        public IReadOnlyList<Vector3> Normals => Model.Normals;

        public (Vector3 A, Vector3 B) GetVertices(Edge edge)
            => Model.GetVertices(edge);

        public Vector3[] GetNormals(Polygon polygon)
            => Model.GetNormals(polygon);

        public Vector3[] GetVertices(Polygon polygon)
            => Model.GetVertices(polygon);
    }
}
