using g3;
using SheetCuttingTools.Abstractions.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Abstractions.Models.FlatGeometry
{
    public class RawFlatGeometry : IFlattenedGeometry
    {
        public IReadOnlyList<Vector2d> Points { get; init; }

        public IReadOnlyList<(Polygon Original, Polygon Placed)> PlacedPolygons { get; init; }

        public IDictionary<Edge, Vector2d> BoundaryNormal { get; init; }

        public Vector2d Center2D { get; init; }

        public IReadOnlyList<Polygon> Polygons { get; init; }

        public IReadOnlyList<Vector3d> Vertices { get; init; }

        public IReadOnlyList<Vector3f> Normals { get; init; }

        public IGeometry? Parent { get; init; }

        public Vector3d Center3d { get; init; }
    }
}
