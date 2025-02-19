using g3;
using SheetCuttingTools.Abstractions.Contracts;

namespace SheetCuttingTools.Abstractions.Models.FlatGeometry
{
    public class RawFlatGeometry : IFlattenedGeometry
    {
        private bool center2dWritten;
        private bool center3dWritten;
        private Vector2d center2d;
        private Vector3d center3d;

        public IReadOnlyList<Vector2d> Points { get; init; }

        public IReadOnlyList<(Polygon Original, Polygon Placed)> PlacedPolygons { get; init; }

        public IDictionary<Edge, Vector2d> BoundaryNormal { get; init; }

        public Vector2d Center2D
        {
            get
            {
                if (!center2dWritten)
                {
                    center2d = Points.Aggregate((a, b) => a + b) / Points.Count;
                    center2dWritten = true;
                }
                return center2d;
            }
        }

        public IReadOnlyList<Polygon> Polygons { get; init; }

        public IReadOnlyList<Vector3d> Vertices { get; init; }

        public IReadOnlyList<Vector3f> Normals { get; init; }

        public IGeometry? Parent { get; init; }

        public Vector3d Center3d
        {
            get
            {
                if (!center3dWritten)
                {
                    center3d = Vertices.Aggregate((a, b) => a + b) / Points.Count;
                    center3dWritten = true;
                }
                return center3d;
            }
        }
    }
}
