using g3;
using SheetCuttingTools.Abstractions.Behaviors;
using SheetCuttingTools.Abstractions.Contracts;
using SheetCuttingTools.Abstractions.Models;
using SheetCuttingTools.Abstractions.Models.FlatGeometry;
using SheetCuttingTools.Infrastructure.Extensions;
using SheetCuttingTools.Infrastructure.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Flattening.Builder
{
    internal class FlattenedGeometryBuilder(IGeometry geometry, IFlattenedSegmentConstraint[] flattenedSegmentConstraints)
    {
        private readonly IGeometry geometry = geometry;
        private readonly IFlattenedSegmentConstraint[] flattenedSegmentConstraints = flattenedSegmentConstraints;

        public Dictionary<Edge, OpenEdgeEntry> OpenEdges { get; set; } = [];

        public Dictionary<Edge, Vector2d> Normals { get; set; } = [];

        public MutableLookup<int, int> Mappings { get; } = new();

        public List<Vector2d> Points { get; set; } = [];

        public HashSet<(Edge Original, Edge Placed)> Boundary { get; } = [];

        public List<(Polygon Original, Polygon Placed)> Polygons { get; set; } = [];

        public bool AddPolygon(Polygon polygon)
        {
            var polygonEdges = polygon.GetEdges().ToArray();
            var numEdges = polygonEdges.Length;
            var (foundExistingEdge, originalEdge, edgeEntry) = polygonEdges
                .Select(edge => (OpenEdges.TryGetValue(edge, out var entry), edge, entry))
                .FirstOrDefault(x => x.Item1 && x.entry.Valid);

            Vector2d normal = new(0, 1);
            Vector2d anchorA, anchorB;
            Vector3d actualA, actualB;
            Edge anchorEdge = edgeEntry.Placed;
            if (foundExistingEdge)
            {
                polygonEdges = ArrayTransform.RotateEdgeArray(polygonEdges, edgeEntry.Original);
                originalEdge = polygonEdges[0];
                (actualA, actualB) = geometry.GetVertices(originalEdge);
                (anchorA, anchorB) = GetEdge(anchorEdge);
                normal = Normals[anchorEdge];
            }
            else
            {
                originalEdge = polygonEdges[0];
                (actualA, actualB) = geometry.GetVertices(originalEdge);
                anchorA = new(0, 0);
                anchorB = new(actualA.Distance(actualB), 0);
                Points.AddRange([anchorA, anchorB]);
                anchorEdge = new(Points.Count - 2, Points.Count - 1);

                Mappings.Add(originalEdge.A, Points.Count - 2);
                Mappings.Add(originalEdge.B, Points.Count - 1);
            }

            (int, Vector2d)[] constructedPoints = new (int, Vector2d)[polygonEdges.Length];
            constructedPoints[0] = (originalEdge.A, anchorA);
            constructedPoints[1] = (originalEdge.B, anchorB);

            double ab = anchorA.Distance(anchorB);

            for (int i = 1; i < numEdges - 1; i++)
            {
                var edge = polygonEdges[i];

                var p = geometry.Vertices[edge.B];

                // X: ab, Y: bc, C: ac
                var triangleSides = new Vector3d(
                    x: ab,
                    y: actualB.Distance(p),
                    z: actualA.Distance(p)
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
                    Segment = geometry,
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

            var center = constructedPoints.Aggregate(Vector2d.Zero, (a, b) => a + b.Item2) / constructedPoints.Length;

            (int flattened, int original)[] indexWhenPlaced = new (int, int)[constructedPoints.Length];

            indexWhenPlaced[0] = (anchorEdge.A, originalEdge.A);
            indexWhenPlaced[1] = (anchorEdge.B, originalEdge.B);


            for (int i = 2; i < indexWhenPlaced.Length; i++)
            {
                int original = constructedPoints[i].Item1;

                bool found = false;
                foreach (var candidate in Mappings[original])
                {
                    if (Points[candidate].EpsilonEqual(constructedPoints[i].Item2, 0.01))
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

                    (Vector2d a, Vector2d b) = GetEdge(mapped);
                    Boundary.Add((original, mapped));
                    var n = -GeometryMath.NormalToLine(center, a, b);
                    Normals[mapped] = n;
                    continue;
                }

                if (value.Placed != mapped)
                {
                    Boundary.Add((original, mapped));
                    (Vector2d a, Vector2d b) = GetEdge(mapped);
                    var n = -GeometryMath.NormalToLine(center, a, b);
                    Normals.TryAdd(mapped, n);
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
        public (Vector2d A, Vector2d B) GetEdge(Edge edge)
           => (Points[edge.A], Points[edge.B]);


        public IFlattenedGeometry ToFlattenedGeometry()
            => new RawFlatGeometry
            {
                Parent = geometry,
                Vertices = geometry.Vertices,
                PlacedPolygons = Polygons.ToArray().AsReadOnly(),
                Polygons = new CollectionMapper<(Polygon Original, Polygon Placed), Polygon>(Polygons.AsReadOnly(), p => p.Original),
                BoundaryNormal = Normals,
                Normals = geometry.Normals,
                Points = Points.ToArray().AsReadOnly()
            };
    }
}
