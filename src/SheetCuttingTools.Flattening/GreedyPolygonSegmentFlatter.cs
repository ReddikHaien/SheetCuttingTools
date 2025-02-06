using SheetCuttingTools.Abstractions.Behaviors;
using SheetCuttingTools.Abstractions.Models;
using SheetCuttingTools.Infrastructure.Extensions;
using SheetCuttingTools.Infrastructure.Math;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace SheetCuttingTools.Flattening
{
    /// <summary>
    /// A greedy flattener that flattens segments based on their polygons.
    /// </summary>
    /// <param name="flattenedSegmentConstraints"></param>
    /// <param name="polygonScorers"></param>
    public class GreedyPolygonSegmentFlatter(IFlattenedSegmentConstraint[] flattenedSegmentConstraints, IPolygonScorer[] polygonScorers, IEdgeFilter[] edgeFilters)
    {
        public IFlattenedSegmentConstraint[] FlattenedSegmentConstraints { get; } = flattenedSegmentConstraints;
        public IPolygonScorer[] PolygonScorers { get; } = polygonScorers;
        public IEdgeFilter[] EdgeFilters { get; } = edgeFilters;

        public FlattenedSegment[] FlattenSegment(Segment segment)
        {
            if (segment.Polygons.Length == 0)
                return [];

            List<FlattenedSegment> segments = [];

            var polys = segment.Polygons.ToList();
            var neighbors = CreateNeighborList(segment);

            var builder = CreateBuilder(segment, polys, neighbors);

            while (polys.Count > 0)
            {
                bool found = false;
                var edges = builder.Edges
                    .Where(x => EdgeFilters.Length == 0 || EdgeFilters.All(y => y.FilterEdge(new(x, segment.Model))))
                    .ToArray();

                foreach (var edge in edges)
                {
                    var neighboringPolygons = neighbors[edge];
                    if (neighboringPolygons.Count == 0)
                        continue;

                    var polygon = neighboringPolygons[0];
                    if (builder.InsertPolygon(in polygon, segment))
                    {
                        polys.Remove(polygon);
                        RemoveNeighbors(in polygon, neighbors);
                        found = true;
                    }
                }

                if (!found)
                {
                    segments.Add(builder.ToFlattenedSegment(segment));
                    builder = CreateBuilder(segment, polys, neighbors);
                }
            }

            if (builder.Polygons.Count > 0)
                segments.Add(builder.ToFlattenedSegment(segment));


            return [.. segments];
        }

        private FlattenedSegmentBuilder CreateBuilder(Segment segment, List<Polygon> polygons, MutableLookup<Edge, Polygon> neighbors)
        {
            var builder = new FlattenedSegmentBuilder(FlattenedSegmentConstraints);
            var maxPoly = PolygonScorers.Length > 0
                    ? polygons.MaxBy(x => PolygonScorers.Average(e => e.ScorePolygon(new PolygonScorerCandidate(x, segment))))
                    : polygons.First();

            builder.InsertPolygon(in maxPoly, segment);
            polygons.Remove(maxPoly);
            RemoveNeighbors(maxPoly, neighbors);

            return builder;
        }

        private static void RemoveNeighbors(in Polygon polygon, MutableLookup<Edge, Polygon> neighbors)
        {
            foreach (var edge in polygon.GetEdges())
            {
                neighbors.RemoveElement(edge, polygon);
            }
        }

        private static MutableLookup<Edge, Polygon> CreateNeighborList(Segment segment)
            => new(segment.Polygons
                .SelectMany(x => x
                    .GetEdges()
                    .Select(e => (edge: e, poly: x)))
                .ToLookup(x => x.edge, x => x.poly));
    }

    public class FlattenedSegmentBuilder(IFlattenedSegmentConstraint[] flattenedSegmentConstraints)
    {
        private readonly IFlattenedSegmentConstraint[] flattenedSegmentConstraints = flattenedSegmentConstraints;

        public Dictionary<Edge, OpenEdgeEntry> OpenEdges { get; set; } = [];

        public Dictionary<Edge, Vector2> Normals { get; set; } = [];

        public List<Vector2> Points { get; set; } = [];

        public HashSet<(Edge Original, Edge Placed)> Boundary = [];

        public List<(Polygon Original, Polygon Placed)> Polygons { get; set; } = [];

        public IEnumerable<Edge> Edges => OpenEdges.Values.Where(x => x.Valid).Select(x => x.Original);

        public bool InsertPolygon(in Polygon polygon, Segment segment)
        {
            var polygonEdges = polygon.GetEdges().ToArray();
            var numEdges = polygonEdges.Length;
            var (foundExistingEdge, originalEdge, edgeEntry) = polygonEdges
                .Select(edge => (OpenEdges.TryGetValue(edge, out var entry), edge, entry))
                .FirstOrDefault(x => x.Item1 && x.entry.Valid);

            Vector2 normal = new(0, 1);
            Vector2 anchorA, anchorB;
            Vector3 actualA, actualB;
            Edge anchorEdge = edgeEntry.Placed;
            if (foundExistingEdge)
            {
                polygonEdges = ArrayTransform.RotateEdgeArray(polygonEdges, edgeEntry.Original);
                originalEdge = polygonEdges[0];
                (actualA, actualB) = segment.Model.GetVertices(originalEdge);
                (anchorA, anchorB) = GetEdge(anchorEdge);
                normal = Normals[anchorEdge];
            }
            else
            {
                originalEdge = polygonEdges[0];
                (actualA, actualB) = segment.Model.GetVertices(originalEdge);
                anchorA = new(0, 0);
                anchorB = new(0, Vector3.Distance(actualA, actualB));
                Points.AddRange([anchorA, anchorB]);
                anchorEdge = new(Points.Count - 2, Points.Count - 1);
            }

            (int, Vector2)[] constructedPoints = new (int, Vector2)[polygonEdges.Length];
            constructedPoints[0] = (anchorEdge.A, anchorA);
            constructedPoints[1] = (anchorEdge.B, anchorB);

            float ab = Vector2.Distance(anchorA, anchorB);

            for (int i = 1; i < numEdges - 1; i++)
            {
                var edge = polygonEdges[i];

                var p = segment.Model.Vertices[edge.B];

                // X: ab, Y: bc, C: ac
                var triangleSides = new Vector3(
                    x: ab,
                    y: Vector3.Distance(actualB, p),
                    z: Vector3.Distance(actualA, p)
                );

                var computed = GeometryMath.ComputeTrianglePoint(anchorA, anchorB, normal, triangleSides);
                constructedPoints[i + 1] = (edge.B, computed);
            }

            if (flattenedSegmentConstraints.Length > 0)
            {
                var candidate = new FlattenedSegmentCandidate
                {
                    AnchorA = anchorA,
                    AnchorB = anchorB,
                    AnchorEdge = anchorEdge,
                    GeneratedPoints = constructedPoints,
                    PlacedPolygon = polygon,
                    Segment = segment,
                };

                if (!flattenedSegmentConstraints.All(x => x.ValidateFlatSegment(in candidate)))
                {
                    OpenEdges[originalEdge] = new OpenEdgeEntry
                    {
                        Original = originalEdge,
                        Placed = anchorEdge,
                        Valid = false
                    };
                    return false;
                }
            }

            var center = constructedPoints.Aggregate(Vector2.Zero, (a, b) => a + b.Item2) / constructedPoints.Length;

            (int flattened, int original)[] indexWhenPlaced =
                Enumerable.Range(Points.Count - 2, constructedPoints.Length)
                .Zip(constructedPoints.Select(x => x.Item1))
                .ToArray();

            indexWhenPlaced[0] = (anchorEdge.A, originalEdge.A);
            indexWhenPlaced[1] = (anchorEdge.B, originalEdge.B);

            Points.AddRange(constructedPoints[2..].Select(x => x.Item2));

            foreach (var (pa, pb) in indexWhenPlaced.SlidingWindow())
            {
                Edge original = new(pa.original, pb.original);
                Edge mapped = new(pa.flattened, pb.flattened);

                if (!OpenEdges.TryGetValue(original, out var value))
                {
                    OpenEdges.Add(original, new OpenEdgeEntry
                    {
                        Original = original,
                        Valid = true,
                        Placed = mapped
                    });

                    (Vector2 a, Vector2 b) = GetEdge(mapped);
                    Normals[mapped] = -GeometryMath.NormalToLine(center, a, b);
                    continue;
                }

                if (value.Placed != mapped)
                {
                    Boundary.Add((original, mapped));
                }
                else
                {
                    Boundary.Remove((original, mapped));
                }
                OpenEdges[original] = new OpenEdgeEntry
                {
                    Original = value.Original,
                    Placed = value.Placed,
                    Valid = false
                };
                Normals.Remove(value.Placed);
            }

            int[] mappedPoints = new int[polygon.Points.Length];
            for (int i = 0, n = mappedPoints.Length; i < n; i++)
            {
                int p = polygon.Points[i];
                var m = indexWhenPlaced.First(x => x.original == p).flattened;
                mappedPoints[i] = m;
            }

            Polygons.Add((polygon, new(mappedPoints)));
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (Vector2 A, Vector2 B) GetEdge(Edge edge)
            => (Points[edge.A], Points[edge.B]);

        public FlattenedSegment ToFlattenedSegment(Segment segment)
            => new()
            {
                Points = [.. Points],
                Polygons = [.. Polygons],
                Segment = segment
            };

    }

    public readonly struct OpenEdgeEntry
    {
        /// <summary>
        /// The corresponding edge in the placed.
        /// </summary>
        public Edge Placed { get; init; }

        /// <summary>
        /// The edge from the input segment.
        /// </summary>
        public Edge Original { get; init; }

        /// <summary>
        /// Wether this edge can be built upon.
        /// </summary>
        public bool Valid { get; init; }
    }
}
