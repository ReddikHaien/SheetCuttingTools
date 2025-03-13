using g3;
using SheetCuttingTools.Abstractions.Behaviors;
using SheetCuttingTools.Abstractions.Contracts;
using SheetCuttingTools.Abstractions.Models;
using SheetCuttingTools.Flattening.Builder;
using SheetCuttingTools.Infrastructure.Extensions;
using SheetCuttingTools.Infrastructure.Progress;
using System.Diagnostics;

using PolygonIndex = int;

namespace SheetCuttingTools.Flattening
{
    public class StripSegmentUnroller2(IFlattenedSegmentConstraint[] flattenedGeometryConstraints, bool direction)
    {

        public IFlattenedGeometry[] UnrollSegment(IGeometry geometry, IProgress<double> progress, CancellationToken cancellationToken = default)
        {
            //List of polygons to be placed.
            List<int> polygons = [.. geometry.Polygons.Select((_, i) => i)];

            //lookup of edges and their associated polygons.
            MutableLookup<Edge, PolygonIndex> edges = new(polygons.SelectMany((x, i) => geometry.Polygons[x].GetEdges().Select(y => (y, i))).ToLookup(keySelector: x => x.y, elementSelector: x => x.i));

            Dictionary<PolygonIndex, int> priority = new(polygons.Select((x, i) => KeyValuePair.Create(i, geometry.Polygons[x].GetEdges().Any(x => edges[x].Count == 1) ? 0 : -1)));


            List<IFlattenedGeometry> geos = [];

            while (true)
            {
                var geo = CreatedStirp(polygons, edges, priority, geometry, cancellationToken);
                if (geo is null)
                    break;
                geos.Add(geo);
            }


            return [.. geos];
        }

        private IFlattenedGeometry? CreatedStirp(List<PolygonIndex> polygons, MutableLookup<Edge, PolygonIndex> edges, Dictionary<PolygonIndex, int> priority, IGeometry geometry, CancellationToken cancellationToken = default)
        {
            PolygonIndex a = FindFirstPolygon(polygons, edges, priority);

            if (a == -1)
            {
                return null;
            }

            var builder = new FlattenedGeometryBuilder(geometry, flattenedGeometryConstraints);
            List<PolygonIndex> added = [];
            int maxPriority = -1;

            builder.AddPolygon(geometry.Polygons[a]);
            added.Add(a);

            maxPriority = Math.Max(maxPriority, priority[a]);

            var b = TakeNextPolygon(a, -1, polygons, edges, priority, geometry);
            
            while (b > -1)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!builder.AddPolygon(geometry.Polygons[b]))
                    break;
                added.Add(b);
                maxPriority = Math.Max(maxPriority, priority[b]);
                RemovePolygonFromState(a, polygons, edges, priority, geometry);
                int o = a;
                a = b;
                b = TakeNextPolygon(a, o, polygons, edges, priority, geometry);
            }

            RemovePolygonFromState(a, polygons, edges, priority, geometry);

            RemoveAndCleanUp(added, edges, priority, geometry, maxPriority);

            return builder.ToFlattenedGeometry();
        }

        private PolygonIndex FindFirstPolygon(List<PolygonIndex> polygons, MutableLookup<Edge, PolygonIndex> edges, Dictionary<PolygonIndex, int> priority)
        {
            return polygons
                .Where(i => priority[i] > -1)
                .OrderBy(i => priority[i])
                .FirstOrDefault(-1);
        }

        private void RemoveAndCleanUp(List<PolygonIndex> addedPolygons, MutableLookup<Edge, PolygonIndex> edges, Dictionary<PolygonIndex, int> priority, IGeometry geometry, int maxPriority)
        {
            foreach (var added in addedPolygons)
            {
                var poly = geometry.Polygons[added];
                foreach (var edge in poly.GetEdges())
                {
                    foreach(var neighbor in edges[edge])
                    {
                        if (neighbor == added)
                            continue;

                        if (priority[neighbor] == -1)
                        {
                            priority[neighbor] = maxPriority + 1;
                        }
                    }
                    edges.RemoveElement(edge, added);
                }
            }



        }

        private void RemovePolygonFromState(PolygonIndex polygon, List<PolygonIndex> polygons, MutableLookup<Edge, PolygonIndex> edges, Dictionary<PolygonIndex, int> priority, IGeometry geometry)
        {
            var poly = geometry.Polygons[polygon];

            polygons.Remove(polygon);

            priority[polygon] = -2;
        }

        private PolygonIndex TakeNextPolygon(int prev, int prevprev, List<PolygonIndex> polygons, MutableLookup<Edge, PolygonIndex> edges, Dictionary<PolygonIndex, int> priority, IGeometry geometry)
        {
            // Who can be placed.
            var candidatesEnumerable = edges
                .Where(x => x.Contains(prev)) // all neighbors
                .SelectMany(x => x) // flatten
                .Where(x => x != prev); // remove previous

            if (prevprev > -1)
            {
                candidatesEnumerable = candidatesEnumerable
                    .Where(c => !geometry.Polygons[prevprev].ContainsSharedPoints(geometry.Polygons[c]));
            }

            var candidates = candidatesEnumerable.ToArray();

            // true  - prioritize inside of model.
            // false - prioritize boundaries of model.

            IEnumerable<PolygonIndex> selector = null!; 
            if (!direction)
            {
                selector = candidates
                    .Where(i => priority[i] >= -1)
                    .Select(i => (i, c: geometry.Polygons[i].GetEdges().Count(x => edges[x].Count == 1)))
                    .OrderByDescending(t => t.c)
                    .Where(t => t.c > 0)
                    .Select(t => t.i);
            }
            else
            {
                selector = candidates
                    .Where(i => priority[i] >= -1)
                    .Select(i => (i, c: geometry.Polygons[i].GetEdges().Count(x => edges[x].Count > 1)))
                    .OrderByDescending(t => t.c)
                    .ThenByDescending(t => priority[t.i])
                    //.Where(t => t.c > 0)
                    .Select(t => t.i);
            }

            var test = candidates
                    .Where(i => priority[i] >= -1)
                    .Select(i => (i, c: geometry.Polygons[i].GetEdges().Count(x => edges[x].Count > 1)))
                    .OrderByDescending(t => t.c)
                    .ThenByDescending(t => priority[t.i])
                    .ToArray();

            return selector
                .FirstOrDefault(-1);
        }
    }

    /// <summary>
    /// A strip unroller
    /// </summary>
    /// <param name="flattenedGeometryConstraints">geometry constraints</param>
    /// <param name="preferredStripDirection">The preferred direction of strips</param>
    /// <param name="treatDirectionAsPlane">Wether the <paramref name="preferredStripDirection"/> should be the normal of the plane the strips should follow</param>
    public class StripSegmentUnroller(IFlattenedSegmentConstraint[] flattenedGeometryConstraints, Vector3d preferredStripDirection, bool treatDirectionAsPlane)
    {
        private readonly IFlattenedSegmentConstraint[] flattenedGeometryConstraints = flattenedGeometryConstraints;
        private readonly Vector3d preferredStripDirection = preferredStripDirection;
        private readonly bool treatDirectionAsPlane = treatDirectionAsPlane;

        public StripSegmentUnroller(IFlattenedSegmentConstraint[] flattenedGeometryConstraints, Vector3d preferredStripDirection) : this(flattenedGeometryConstraints, preferredStripDirection, false)
        {

        }

        public StripSegmentUnroller(IFlattenedSegmentConstraint[] flattenedGeometryConstraints) : this(flattenedGeometryConstraints, Vector3d.AxisZ, false)
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
            var (first, second) = FindInitialPolygons(polygons, edges, geometry);

            if (first == -1 || second == -1)
                return null!;
            
            var firstPoly = polygons[first];
            var secondPoly = polygons[second];

            //var first = FindBestFit(polygons, edges);

            //var candidates = firstPoly.GetEdges().SelectMany(e => edges[e]).Where(x => x != first).ToArray();

            //var second = FindBestFit(polygons, edges, candidates);

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
                int[] candidates = null!;
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

        public (int, int) FindInitialPolygons(List<Polygon> polygons, ILookup<Edge, int> edges, IGeometry geometry)
        {
            var goodCandidates = FindPolygonsWithOpenEdges(polygons, edges);

            foreach(var firstIndex in goodCandidates)
            {
                Edge firstBest = default;
                Edge secondBest = default;
                double bestDot = treatDirectionAsPlane ? 1.1 : -0.1;

                var first = polygons[firstIndex];
                var mid = geometry.GetMidPoint3d(first);
                var firstEdges = first.GetEdges().ToArray();
                foreach(var a in firstEdges)
                {
                    var aMid = geometry.GetMidPoint3d(a);

                    var fdir = (mid - aMid).Normalized;

                    foreach(var b in firstEdges)
                    {
                        if (a == b)
                            continue;

                        var bmid = geometry.GetMidPoint3d(b);

                        var sdir = (bmid - mid).Normalized;

                        if (Math.Abs(fdir.Dot(sdir)) < 0.9)
                            continue;

                        double dot = Math.Abs(fdir.Dot(preferredStripDirection));
                        if (treatDirectionAsPlane ? dot < bestDot : dot > bestDot)
                        {
                            firstBest = a;
                            secondBest = b;
                            bestDot = dot;
                        }
                    }
                }

                if (firstBest == default)
                    continue;

                var candidate = edges[firstBest].Concat(edges[secondBest]).FirstOrDefault(x => x != firstIndex, -1);
                if (candidate is -1)
                    continue;

                return (firstIndex, candidate);
            }

            return (-1, -1);
        }

        private int[] FindPolygonsWithOpenEdges(List<Polygon> polygons, ILookup<Edge, int> lookup, int[] candidates = null!)
        {
            var polys = candidates is null
            ? polygons.Select((x, i) => (x, i))
            : candidates.Select(x => (polygons[x], x));

            return polys.Select(x => (index: x.Item2, count: x.Item1.GetEdges().Count(e => lookup[e].Count() == 1)))
                .OrderByDescending(x => x.count)
                .Select(x => x.index).ToArray();
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
