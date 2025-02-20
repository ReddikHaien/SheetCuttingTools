using g3;
using SheetCuttingTools.Abstractions.Behaviors;
using SheetCuttingTools.Abstractions.Contracts;
using SheetCuttingTools.Abstractions.Models;
using SheetCuttingTools.Flattening.Builder;
using SheetCuttingTools.Infrastructure.Extensions;
using System.Diagnostics;

namespace SheetCuttingTools.Flattening
{
    public class StripSegmentUnroller(IFlattenedSegmentConstraint[] flattenedGeometryConstraints, Vector3d preferredStripDirection)
    {
        private readonly IFlattenedSegmentConstraint[] flattenedGeometryConstraints = flattenedGeometryConstraints;
        private readonly Vector3d preferredStripDirection = preferredStripDirection;

        public StripSegmentUnroller(IFlattenedSegmentConstraint[] flattenedGeometryConstraints) : this(flattenedGeometryConstraints, Vector3d.AxisZ)
        {

        }

        public StripSegmentUnroller(): this([])
        {

        }


        public IFlattenedGeometry[] UnrollSegment(IGeometry geometry, CancellationToken cancellationToken = default)
        {
            List<Polygon> polygons = [.. geometry.Polygons];

            List<IFlattenedGeometry> strips = [];

            while (polygons.Count > 0 && !cancellationToken.IsCancellationRequested)
            {
                var strip = CreateStrip(polygons, geometry, cancellationToken);
                if (strip is null)
                    break;
                strips.Add(strip);
            }


            return [.. strips];
        }

        private IFlattenedGeometry CreateStrip(List<Polygon> polygons, IGeometry geometry, CancellationToken cancellationToken = default)
        {
            var builder = new FlattenedGeometryBuilder(geometry, flattenedGeometryConstraints);

            ILookup<Edge, int> edges = polygons.SelectMany((x, i) => x.GetEdges().Select(y => (y, i))).ToLookup(keySelector: x => x.y, elementSelector: x => x.i);
            var first = FindBestFit(polygons, edges);
            var firstPoly = polygons[first];

            var candidates = firstPoly.GetEdges().SelectMany(e => edges[e]).Where(x => x != first).ToArray();

            var second = FindBestFit(polygons, edges, candidates);
            var secondPoly = polygons[second];

            if (!builder.AddPolygon(firstPoly) || !builder.AddPolygon(secondPoly))
            {
                Debug.WriteLine("Can't add initial values to builder!");
                return null!;
            }

            if (first > second)
            {
                polygons.RemoveAt(first);
                polygons.RemoveAt(second);
            }
            else
            {
                polygons.RemoveAt(second);
                polygons.RemoveAt(first);
            }

            var sharedEdge = firstPoly.GetEdges().Intersect(secondPoly.GetEdges()).FirstOrDefault();
            if (sharedEdge == new Edge(0, 0))
                return null!;

            List<Polygon> strip = [firstPoly, secondPoly];
            List<int> indexStrip = [first, second];
            edges = polygons.SelectMany((x, i) => x.GetEdges().Select(y => (y, i))).ToLookup(keySelector: x => x.y, elementSelector: x => x.i);

            Edge firstEdge = default;
            Edge secondEdge = default;
            double bestF = double.MinValue;

            var (hei, hade) = geometry.GetVertices(sharedEdge);

            Vector3d midPoint = (hei + hade) / 2;

            foreach (var edgeF in firstPoly.GetEdges())
            {
                if (edgeF == sharedEdge) continue;
                var (fa, fb) = geometry.GetVertices(edgeF);
                var fm = (fa + fb) / 2;
                foreach (var edgeS in secondPoly.GetEdges().Where(x => edges[x].Any()))
                {
                    if (edgeS == sharedEdge) continue;
                    var (sa, sb) = geometry.GetVertices(edgeS);

                    var sm = (sa + sb) / 2;

                    var d1 = midPoint - fm;
                    d1.Normalize();
                    var d2 = sm - midPoint;
                    d2.Normalize();
                    var dot = d1.Dot(d2);
                    if (dot > bestF)
                    {
                        firstEdge = edgeF;
                        secondEdge = edgeS;
                        bestF = dot;
                    }
                }
            }

            while (!cancellationToken.IsCancellationRequested)
            {

                Polygon next = default;
                if (strip[^1].Points.Length == 3)
                {
                    candidates = edges[secondEdge].ToArray();
                    if (candidates.Length == 0)
                        break;
                    

                    next = polygons[candidates[0]];
                    var nextEdge = FindNextEdge(sharedEdge, secondEdge, next, geometry);
                    firstEdge = sharedEdge;
                    sharedEdge = secondEdge;
                    secondEdge = nextEdge;
                }
                else
                {
                    Edge opposite = strip[^1].GetEdges().FirstOrDefault(x => !x.ContainsPoint(sharedEdge.A) && !x.ContainsPoint(sharedEdge.B));

                    if (opposite == new Edge(0, 0))
                        break;

                    candidates = edges[opposite].ToArray();
                    if (candidates.Length == 0)
                        break;

                    next = polygons[candidates[0]];
                }

                if (!builder.AddPolygon(next))
                    break;

                strip.Add(next);
                polygons.RemoveAt(candidates[0]);
                edges = polygons.SelectMany((x, i) => x.GetEdges().Select(y => (y, i))).ToLookup(keySelector: x => x.y, elementSelector: x => x.i);
                sharedEdge = next.GetEdges().Intersect(strip[^2].GetEdges()).FirstOrDefault();
                if (sharedEdge == new Edge(0, 0))
                    break;
            }

            return builder.ToFlattenedGeometry();
        }

        private int FindBestFit(List<Polygon> polygons, ILookup<Edge, int> lookup, int[] candidates = null!)
        {
            int best = 0;
            int count = -1;

            var polys = candidates is null
                ? polygons.Select((x, i) => (x, i))
                : candidates.Select(x => (polygons[x], x));

            foreach (var (p, i) in polys)
            {
                var c = p.GetEdges().Count(e => lookup[e].Count() == 1);
                if (c > count)
                {
                    best = i;
                    count = c;
                }
            }

            return best;
        }

        private static Edge FindNextEdge(Edge first, Edge shared, Polygon polygon, IGeometry geometry)
        {
            var (sharedA, sharedB) = geometry.GetVertices(shared);
            var sharedMidpoint = (sharedA + sharedB) / 2;

            var (firstA, firstB) = geometry.GetVertices(first);            
            var firstMidpoint = (firstA + firstB) / 2;

            var dir1 = sharedMidpoint-firstMidpoint;
            dir1.Normalize();


            Edge bestEdge = default;
            double bestScore = double.MinValue;

            foreach (var edgeS in polygon.GetEdges())
            {
                if (edgeS == first) continue;
                var (sa, sb) = geometry.GetVertices(edgeS);

                var sm = (sa + sb) / 2;

                var d2 = sm - sharedMidpoint;
                d2.Normalize();
                var dot = dir1.Dot(d2);
                if (dot > bestScore)
                {
                    bestEdge = edgeS;
                    bestScore = dot;
                }
            }

            return bestEdge;
        }
    }
}
