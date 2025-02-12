using SheetCuttingTools.Abstractions.Behaviors;
using SheetCuttingTools.Abstractions.Contracts;
using SheetCuttingTools.Abstractions.Models;
using SheetCuttingTools.Abstractions.Models.Numerics;
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
    public class GreedyPolygonSegmentUnroller(IFlattenedSegmentConstraint[] flattenedSegmentConstraints, IPolygonScorer[] polygonScorers, IEdgeFilter[] edgeFilters)
    {
        public IFlattenedSegmentConstraint[] FlattenedSegmentConstraints { get; } = flattenedSegmentConstraints;
        public IPolygonScorer[] PolygonScorers { get; } = polygonScorers;
        public IEdgeFilter[] EdgeFilters { get; } = edgeFilters;

        public FlattenedSegment[] FlattenSegment(IGeometryProvider segment, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (segment.Polygons.Count == 0)
                return [];

            List<FlattenedSegment> segments = [];

            var polys = segment.Polygons.ToList();
            var neighbors = CreateNeighborList(segment);

            var builder = CreateBuilder(segment, polys, neighbors);

            while (polys.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                bool found = false;
                var edges = builder.Edges
                    .Where(x => EdgeFilters.Length == 0 || EdgeFilters.All(y => y.FilterEdge(new(x, segment))))
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

        private FlattenedSegmentBuilder CreateBuilder(IGeometryProvider segment, List<Polygon> polygons, MutableLookup<Edge, Polygon> neighbors)
        {
            var builder = new FlattenedSegmentBuilder(FlattenedSegmentConstraints);
            var maxPoly = polygons.MaxByMany(PolygonScorers, segment);

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

        private static MutableLookup<Edge, Polygon> CreateNeighborList(IGeometryProvider segment)
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

        public Dictionary<Edge, HighPresVector2> Normals { get; set; } = [];

        MutableLookup<int, int> Mappings = new();

        public List<HighPresVector2> Points { get; set; } = [];

        public HashSet<(Edge Original, Edge Placed)> Boundary = [];

        public List<(Polygon Original, Polygon Placed)> Polygons { get; set; } = [];

        public IEnumerable<Edge> Edges => OpenEdges.Values.Where(x => x.Valid).Select(x => x.Original);

        public bool InsertPolygon(in Polygon polygon, IGeometryProvider segment)
        {
            var polygonEdges = polygon.GetEdges().ToArray();
            var numEdges = polygonEdges.Length;
            var (foundExistingEdge, originalEdge, edgeEntry) = polygonEdges
                .Select(edge => (OpenEdges.TryGetValue(edge, out var entry), edge, entry))
                .FirstOrDefault(x => x.Item1 && x.entry.Valid);

            HighPresVector2 normal = new(0, 1);
            HighPresVector2 anchorA, anchorB;
            Vector3 actualA, actualB;
            Edge anchorEdge = edgeEntry.Placed;
            if (foundExistingEdge)
            {
                polygonEdges = ArrayTransform.RotateEdgeArray(polygonEdges, edgeEntry.Original);
                originalEdge = polygonEdges[0];
                (actualA, actualB) = segment.GetVertices(originalEdge);
                (anchorA, anchorB) = GetEdge(anchorEdge);
                normal = Normals[anchorEdge];
            }
            else
            {
                originalEdge = polygonEdges[0];
                (actualA, actualB) = segment.GetVertices(originalEdge);
                anchorA = new(0, 0);
                anchorB = new(Vector3.Distance(actualA, actualB), 0);
                Points.AddRange([anchorA, anchorB]);
                anchorEdge = new(Points.Count - 2, Points.Count - 1);

                Mappings.Add(originalEdge.A, Points.Count - 2);
                Mappings.Add(originalEdge.B, Points.Count - 1);
            }

            (int, HighPresVector2)[] constructedPoints = new (int, HighPresVector2)[polygonEdges.Length];
            constructedPoints[0] = (originalEdge.A, anchorA);
            constructedPoints[1] = (originalEdge.B, anchorB);

            double ab = HighPresVector2.Distance(anchorA, anchorB);

            for (int i = 1; i < numEdges - 1; i++)
            {
                var edge = polygonEdges[i];

                var p = segment.Vertices[edge.B];

                // X: ab, Y: bc, C: ac
                var triangleSides = new HighPresVector3(
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
                    FlattenedPoints = Points,
                    Boundary = Boundary
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

            var center = constructedPoints.Aggregate(HighPresVector2.Zero, (a, b) => a + b.Item2) / constructedPoints.Length;

            (int flattened, int original)[] indexWhenPlaced = new (int, int)[constructedPoints.Length];

            indexWhenPlaced[0] = (anchorEdge.A, originalEdge.A);
            indexWhenPlaced[1] = (anchorEdge.B, originalEdge.B);


            for (int i = 2; i < indexWhenPlaced.Length; i++)
            {
                int original = constructedPoints[i].Item1;
                
                bool found = false;
                foreach(var candidate in Mappings[original])
                {
                    if (HighPresVector2.EpsilonEquals(Points[candidate], constructedPoints[i].Item2))
                    {
                        indexWhenPlaced[i] = (candidate, original);
                        found = true;
                        break;
                    }
                }

                if (found)
                    continue;
                
                Points.Add(constructedPoints[i].Item2);

                Mappings.Add(original, Points.Count - 1);

                indexWhenPlaced[i] = (Points.Count - 1, original);
            }

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

                    (HighPresVector2 a, HighPresVector2 b) = GetEdge(mapped);
                    Boundary.Add((original, mapped));
                    Normals[mapped] = -GeometryMath.NormalToLine(center, a, b);
                    continue;
                }

                if (value.Placed != mapped)
                {
                    Boundary.Add((original, mapped));
                    (HighPresVector2 a, HighPresVector2 b) = GetEdge(mapped);
                    Normals.TryAdd(mapped, -GeometryMath.NormalToLine(center, a, b));
                }
                else
                {
                    Boundary.Remove((original, mapped));
                    Normals.Remove(mapped);
                }
                OpenEdges[original] = new OpenEdgeEntry
                {
                    Original = value.Original,
                    Placed = value.Placed,
                    Valid = false
                };
                
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
        public (HighPresVector2 A, HighPresVector2 B) GetEdge(Edge edge)
            => (Points[edge.A], Points[edge.B]);

        public FlattenedSegment ToFlattenedSegment(IGeometryProvider segment)
            => new()
            {
                Points = [.. Points],
                Polygons = [.. Polygons],
                Segment = segment,
                Normals = Normals.ToDictionary(x => x.Key, x => (Vector2)x.Value)
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
