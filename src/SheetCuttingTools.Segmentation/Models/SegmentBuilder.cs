using g3;
using SheetCuttingTools.Abstractions.Behaviors;
using SheetCuttingTools.Abstractions.Contracts;
using SheetCuttingTools.Abstractions.Models;
using SheetCuttingTools.Abstractions.Models.Geometry;
using SheetCuttingTools.Infrastructure.Math;

namespace SheetCuttingTools.Segmentation.Models
{
    /// <summary>
    /// Segment builder class
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="constraints"></param>
    public class SegmentBuilder(IGeometry parent, ISegmentConstraint[] constraints) : IGeometry
    {
        public IGeometry Parent { get; } = parent;

        public ISegmentConstraint[] Constraints { get; } = constraints;

        public List<Polygon> Polygons { get; set; } = [];

        public HashSet<int> AddedPoints { get; set; } = [];

        private readonly List<Edge> boundary = [];

        public IReadOnlyList<Edge> Boundary => boundary;

        public Guid Id { get; set; } = Guid.NewGuid();

        public Vector3d Center3d { get; set; } = Vector3d.Zero;

        public bool AddPolygon(Polygon polygon, Edge commonEdge)
        {
            Polygons.Add(polygon);

            if (Constraints.Length > 0 && Polygons.Count > 1)
            {
                var candidate = new SegmentCandidate(Id, Polygons, this);
                if (!Constraints.All(x => x.ValidateSegment(candidate)))
                {
                    Polygons.RemoveAt(Polygons.Count - 1);
                    return false;
                }
            }

            var toAdd = polygon.Points.Where(x => !AddedPoints.Contains(x)).ToArray();

            if (toAdd.Length > 0)
            {
                Center3d *= AddedPoints.Count;
                Center3d += toAdd.Select(x => Parent.Vertices[x]).Aggregate((a, b) => a + b);
                Center3d /= AddedPoints.Count + toAdd.Length;
            }

            foreach (var x in toAdd)
                AddedPoints.Add(x);

            UpdateBoundary(polygon, commonEdge);

            return true;
        }

        public void UpdateBoundary(Polygon polygon, Edge commonEdge)
        {

            var index = boundary.IndexOf(commonEdge);

            if (index > -1)
            {
                var inBoundEdge = boundary[index];
                var edges = ArrayTransform.RotateEdgeArray(polygon.GetEdges().ToArray(), commonEdge);
                var addedBound = edges.Where(x => x != commonEdge).ToArray();
                boundary.RemoveAt(index);

                addedBound = ArrayTransform.GetEdgeArrayInBounds(addedBound, inBoundEdge.A, inBoundEdge.B);

                boundary.InsertRange(index, addedBound);
            }
            else
            {
                boundary.AddRange(polygon.GetEdges());
            }
        }

        public IGeometry ToSegment(IGeometry? segment)
            => new RawGeometry
            {
                Parent = segment ?? Parent,
                Normals = Parent.Normals,
                Vertices = Parent.Vertices,
                Polygons = [.. Polygons],
            };

        IReadOnlyList<Polygon> IGeometry.Polygons => Polygons;

        IReadOnlyList<Vector3d> IGeometry.Vertices => Parent.Vertices;

        IReadOnlyList<Vector3f> IGeometry.Normals => Parent.Normals;
    }
}
