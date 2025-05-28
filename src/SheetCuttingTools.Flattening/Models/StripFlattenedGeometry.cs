using g3;
using SheetCuttingTools.Abstractions.Contracts;
using SheetCuttingTools.Abstractions.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Flattening.Models
{
    public sealed record StripFlattenedGeometry : IFlattenedGeometry
    {
        public StripFlattenedGeometry(IFlattenedGeometry inner, Dictionary<Edge, int> edgeKinds)
        {
            Inner = inner;
            EdgeKinds = edgeKinds;
        }

        public IReadOnlyList<Vector2d> Points => Inner.Points;

        public IReadOnlyList<(Polygon Original, Polygon Placed)> PlacedPolygons => Inner.PlacedPolygons;

        public IDictionary<Edge, Vector2d> BoundaryNormal => Inner.BoundaryNormal;

        public Vector2d Center2D => Inner.Center2D;

        public IReadOnlyList<Polygon> Polygons => Inner.Polygons;

        public IReadOnlyList<Vector3d> Vertices => Inner.Vertices;

        public IReadOnlyList<Vector3f> Normals => Inner.Normals;

        public IGeometry? Parent => Inner.Parent;

        public Vector3d Center3d => Inner.Center3d;

        /// <summary>
        /// The actual flattened geometry.
        /// </summary>
        public IFlattenedGeometry Inner { get; }
        
        /// <summary>
        /// The edge kinds for each flattened edge.
        /// </summary>
        public Dictionary<Edge, int> EdgeKinds { get; }
    }
}
