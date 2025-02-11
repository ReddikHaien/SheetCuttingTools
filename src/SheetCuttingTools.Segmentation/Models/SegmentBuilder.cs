using SheetCuttingTools.Abstractions.Behaviors;
using SheetCuttingTools.Abstractions.Contracts;
using SheetCuttingTools.Abstractions.Models;
using SheetCuttingTools.Infrastructure.Math;
using System.Numerics;

namespace SheetCuttingTools.Segmentation.Models
{
    /// <summary>
    /// Segment builder class
    /// </summary>
    /// <param name="model"></param>
    /// <param name="constraints"></param>
    public class SegmentBuilder(IGeometryProvider model, ISegmentConstraint[] constraints) : IGeometryProvider
    {
        public IGeometryProvider Model { get; } = model;

        public ISegmentConstraint[] Constraints { get; } = constraints;

        public List<Polygon> Polygons { get; set; } = [];

        public HashSet<int> AddedPoints { get; set; } = [];

        private readonly List<Edge> boundary = [];

        public IReadOnlyList<Edge> Boundary => boundary;

        public Guid Id { get; set; } = Guid.NewGuid();

        public Vector3 Center { get; set; } = Vector3.Zero;

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

            if(toAdd.Length > 0)
            {
                Center *= AddedPoints.Count;
                Center += toAdd.Select(x => Model.Vertices[x]).Aggregate(Vector3.Add);
                Center /= AddedPoints.Count + toAdd.Length;
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

        public Segment ToSegment(Segment? segment)
            => new(Model)
            {
                Id = Id,
                Parent = segment,
                Polygons = [.. Polygons],
            };

        IReadOnlyList<Polygon> IGeometryProvider.Polygons => Polygons;

        IReadOnlyList<Vector3> IGeometryProvider.Vertices => Model.Vertices;

        IReadOnlyList<Vector3> IGeometryProvider.Normals => Model.Normals;

        (Vector3 A, Vector3 B) IGeometryProvider.GetVertices(Edge edge)
            => Model.GetVertices(edge);

        Vector3[] IGeometryProvider.GetNormals(Polygon polygon)
            => Model.GetNormals(polygon);

        Vector3[] IGeometryProvider.GetVertices(Polygon polygon)
            => Model.GetVertices(polygon);
    }
}
