using SheetCuttingTools.Abstractions.Contracts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Abstractions.Models
{

    [Obsolete("Replaced by IFlattenedGeometry")]
    public class FlattenedSegment : IGeometryProvider
    {
        public IGeometryProvider Segment { get; set; } = null!;

        public Vector2[] Points { get; set; } = [];

        public (Polygon Original, Polygon Placed)[] Polygons { get; set; } = [];

        /// <summary>
        /// Boundary normals
        /// </summary>
        public Dictionary<Edge, Vector2> Normals { get; set; } = [];

        public IReadOnlyList<Vector3> Vertices => Segment.Vertices;

        public Vector3 Center { get; set; }

        IReadOnlyList<Polygon> IGeometryProvider.Polygons => new PolygonVector(Polygons);

        IReadOnlyList<Vector3> IGeometryProvider.Normals => Segment.Normals;

        public (Vector2, Vector2) GetEdge(Edge edge)
        => (Points[edge.A], Points[edge.B]);
    }

    internal readonly struct PolygonVector((Polygon Original, Polygon Placed)[] values) : IReadOnlyList<Polygon>
    {
        public Polygon this[int index] => values[index].Original;

        public int Count => values.Length;

        public IEnumerator<Polygon> GetEnumerator()
            => values.Select(x => x.Original).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }
}
