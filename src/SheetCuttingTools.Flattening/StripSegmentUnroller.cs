using g3;
using SheetCuttingTools.Abstractions.Behaviors;
using SheetCuttingTools.Abstractions.Contracts;
using SheetCuttingTools.Abstractions.Models;
using SheetCuttingTools.Flattening.Builder;
using SheetCuttingTools.Flattening.Models;
using SheetCuttingTools.Infrastructure.Extensions;
using SheetCuttingTools.Infrastructure.Math;
using System.Diagnostics;

using PolygonIndex = int;

namespace SheetCuttingTools.Flattening
{
    public class StripSegmentUnroller3(IFlattenedSegmentConstraint[] flattenedGeometryConstraints, bool flip)
    {
        public IFlattenedGeometry[] UnrollSegment(IGeometry geometry, IProgress<double> progress, CancellationToken cancellationToken = default)
        {
            //List of polygons to be placed.
            List<int> polygons = [.. geometry.Polygons.Select((_, i) => i)];

            //lookup of edges and their associated polygons.
            MutableLookup<Edge, PolygonIndex> edges = new(
                polygons
                .SelectMany((x, i) => geometry.Polygons[x]
                    .GetEdges()
                    .Select(y => (y, i)))
                .ToLookup(keySelector: x => x.y, elementSelector: x => x.i));

            Dictionary<PolygonIndex, int> priority = new(
                polygons
                .Select((x, i) =>
                KeyValuePair
                .Create(i,
                    geometry.Polygons[x]
                        .GetEdges()
                        .Any(x => edges[x].Count == 1) ? 0 : -1)));

            Dictionary<PolygonIndex, int> stripId = [];

            List<IFlattenedGeometry> geos = [];

            var sw = Stopwatch.StartNew();

            while (true)
            {
                var geo = CreatedStrip(polygons, edges, priority, geometry, stripId, cancellationToken);
                if (!geo)
                    break;
                //geos.AddRange(geo);
            }

            sw.Stop();
            var stripTime = sw.ElapsedMilliseconds;
            Debug.WriteLine($"Completed strip unroll: strip: {stripTime}ms");
            sw.Restart();
            var result = flip
                ? GetStripsAlong(stripId, geometry)
                : GetStripsAcross(stripId, geometry);

            sw.Stop();
            var resultTime = sw.ElapsedMilliseconds;
            Debug.WriteLine($"Completed strip unroll: reconstruct: {resultTime}ms");

            return result;
        }

        private IFlattenedGeometry[] GetStripsAlong(Dictionary<PolygonIndex, int> stripId, IGeometry geometry)
        {
            var neighbourSet = geometry.Polygons
                .Select((p, i)
                    => (
                        polygon: i,
                        neighbors: geometry.Polygons
                            .Select((x, i) => (polygon: x, index: i))
                            .Where(b => b.index != i)
                            .Where((b) => b.polygon.ContainsSharedEdge(p))
                            .Select(b => b.index)
                            .ToArray()
                        )
                    ).ToDictionary(x => x.polygon, x => x.neighbors);

            List<PolygonIndex[]> strips = [];
            List<(int, PolygonIndex[][])> strips2 = [];
            while (neighbourSet.Count > 0)
            {
                List<Edge> chain = [];

                List<PolygonIndex> strip = [];

                var p = neighbourSet.First().Key;

                List<PolygonIndex> toAddStack = [p];
                int FId = 0;
                while (toAddStack.Count > 0)
                {
                    var poly = toAddStack.Last();
                    toAddStack.RemoveAt(toAddStack.Count - 1);

                    int id = stripId[poly];
                    if (id == 0)
                        continue;

                    if (FId == 0)
                        FId = id;

                    var next = neighbourSet[poly].Where(p => stripId[p] == id && stripId[p] > 0).ToArray();

                    foreach (var e in next)
                    {
                        chain.Add(new(poly, e));
                        toAddStack.Add(e);
                    }

                    neighbourSet.Remove(poly);
                    stripId[poly] = 0;
                }


                strips2.Add((FId, ArrayTransform.CreateEdgeLoops([.. chain])));
            }

            strips = strips2.OrderBy(x => x.Item1).SelectMany(x => x.Item2).ToList();

            List<IFlattenedGeometry> split = [];


            foreach (var strip in strips)
            {
                FlattenedGeometryBuilder builder = new(geometry, flattenedGeometryConstraints);
                Dictionary<Edge, int> edgeKinds = [];

                int length = strip.First() == strip.Last()
                    ? strip.Length - 1
                    : strip.Length;

                for (int i = 0; i < length; i++)
                {
                    var polygon = geometry.Polygons[strip[i]];

                    if (!builder.AddPolygon(polygon, out var placed))
                    {
                        if (builder.Polygons.Count > 0)
                            split.Add(ToFlattenedStripGeometry(builder, edgeKinds));
                        builder = new(geometry, flattenedGeometryConstraints);
                        edgeKinds = [];

                        if (!builder.AddPolygon(polygon, out placed))
                            throw new InvalidOperationException("Unable to place initial polygon!");
                    }

                    Edge[] sharedEdges = new Edge[placed.Value.Points.Length];
                    int counter = 0;
                    foreach (var e2 in placed.Value.GetEdges())
                    {
                        // if an edge is already added, mark it as interior(1).
                        if (!edgeKinds.TryAdd(e2, 0))
                        {
                            edgeKinds[e2] = 1;
                            sharedEdges[counter++] = e2;
                        }
                    }

                    for (int j = 0; j < counter; j++)
                    {
                        Edge e2 = sharedEdges[j];

                        //Mark all connecting edges to an interior edge as side edge(2)
                        foreach (var sideEdge in edgeKinds.Keys.Where(e => e != e2 && e.HasSharedPoint(e2)))
                        {
                            edgeKinds[sideEdge] = 2;
                        }
                    }

                }
                if (builder.Polygons.Count > 0)
                    split.Add(ToFlattenedStripGeometry(builder, edgeKinds));

            }

            return [.. split];
        }

        private IFlattenedGeometry[] GetStripsAcross(Dictionary<PolygonIndex, int> stripId, IGeometry geometry)
        {
            var neighbourSet = geometry.Polygons
                .Select((p, i)
                    => (
                        polygon: i,
                        neighbors: geometry.Polygons
                            .Select((x, i) => (polygon: x, index: i))
                            .Where(b => b.index != i)
                            .Where((b) => b.polygon.ContainsSharedEdge(p))
                            .Select(b => b.index)
                            .ToArray()
                        )
                    ).ToDictionary(x => x.polygon, x => x.neighbors);

            List<PolygonIndex[]> strips = [];

            while (neighbourSet.Count > 0)
            {
                List<Edge> chain = [];

                List<PolygonIndex> strip = [];

                var p = neighbourSet.First().Key;

                List<PolygonIndex> toAddStack = [p];

                while (toAddStack.Count > 0)
                {
                    var poly = toAddStack.Last();
                    toAddStack.RemoveAt(toAddStack.Count - 1);

                    int id = stripId[poly];
                    if (id == 0)
                        continue;

                    var next = neighbourSet[poly].Where(p => stripId[p] != id && stripId[p] > 0).ToArray();

                    foreach (var e in next)
                    {
                        chain.Add(new(poly, e));
                        toAddStack.Add(e);
                    }

                    neighbourSet.Remove(poly);
                    stripId[poly] = 0;
                }

                strips.AddRange(ArrayTransform.CreateEdgeLoops(chain.ToArray()));
            }


            List<IFlattenedGeometry> split = [];


            foreach (var strip in strips)
            {
                FlattenedGeometryBuilder builder = new(geometry, flattenedGeometryConstraints);
                Dictionary<Edge, int> edgeKinds = [];

                int length = strip.First() == strip.Last()
                    ? strip.Length - 1
                    : strip.Length;

                for (int i = 0; i < length; i++)
                {
                    var polygon = geometry.Polygons[strip[i]];

                    if (!builder.AddPolygon(polygon, out var placed))
                    {
                        if (builder.Polygons.Count > 0)
                            split.Add(ToFlattenedStripGeometry(builder, edgeKinds));
                        builder = new(geometry, flattenedGeometryConstraints);
                        edgeKinds = [];

                        if (!builder.AddPolygon(polygon, out placed))
                            throw new InvalidOperationException("Unable to place initial polygon!");
                    }

                    Edge[] sharedEdges = new Edge[placed.Value.Points.Length];
                    int counter = 0;
                    foreach (var e2 in placed.Value.GetEdges())
                    {
                        // if an edge is already added, mark it as interior(1).
                        if (!edgeKinds.TryAdd(e2, 0))
                        {
                            edgeKinds[e2] = 1;
                            sharedEdges[counter++] = e2;
                        }
                    }

                    for (int j = 0; j < counter; j++)
                    {
                        Edge e2 = sharedEdges[j];

                        //Mark all connecting edges to an interior edge as side edge(2)
                        foreach (var sideEdge in edgeKinds.Keys.Where(e => e != e2 && e.HasSharedPoint(e2)))
                        {
                            edgeKinds[sideEdge] = 2;
                        }
                    }

                }
                if (builder.Polygons.Count > 0)
                    split.Add(ToFlattenedStripGeometry(builder, edgeKinds));

            }

            return [.. split];
        }

        private bool CreatedStrip(List<PolygonIndex> polygons, MutableLookup<Edge, PolygonIndex> edges, Dictionary<PolygonIndex, int> priority, IGeometry geometry, Dictionary<PolygonIndex, int> stripId, CancellationToken cancellationToken = default)
        {

            PolygonIndex a = FindFirstPolygon(polygons, edges, priority);

            if (a == -1)
            {
                return false;
            }

            List<PolygonIndex> added = [];
            int maxPriority = -1;

            added.Add(a);

            maxPriority = Math.Max(maxPriority, priority[a]);

            var b = TakeNextPolygon(a, -1, polygons, edges, priority, geometry);

            while (b > -1)
            {
                cancellationToken.ThrowIfCancellationRequested();

                added.Add(b);

                maxPriority = Math.Max(maxPriority, priority[b]);
                RemovePolygonFromState(a, polygons, edges, priority, geometry);
                int o = a;
                a = b;
                b = TakeNextPolygon(a, o, polygons, edges, priority, geometry);
            }

            RemovePolygonFromState(a, polygons, edges, priority, geometry);

            RemoveAndCleanUp(added, edges, priority, geometry, maxPriority);

            int newStripId = stripId.Count + 1;
            foreach (var aaa in added)
            {
                if (!stripId.TryAdd(aaa, newStripId))
                    throw new InvalidOperationException("Polygon has been added multiple times");
            }

            return true;
        }

        private static IFlattenedGeometry ToFlattenedStripGeometry(FlattenedGeometryBuilder builder, Dictionary<Edge, int> edgeKinds)
            => new StripFlattenedGeometry(builder.ToFlattenedGeometry(), edgeKinds);

        private PolygonIndex FindFirstPolygon(List<PolygonIndex> polygons, MutableLookup<Edge, PolygonIndex> edges, Dictionary<PolygonIndex, int> priority)
        {
            return polygons
                .Where(i => priority[i] > -1)
                .OrderBy(i => priority[i])
                .FirstOrDefault(-1);
        }

        private static void RemoveAndCleanUp(List<PolygonIndex> addedPolygons, MutableLookup<Edge, PolygonIndex> edges, Dictionary<PolygonIndex, int> priority, IGeometry geometry, int maxPriority)
        {
            foreach (var added in addedPolygons)
            {
                var poly = geometry.Polygons[added];
                foreach (var edge in poly.GetEdges())
                {
                    foreach (var neighbor in edges[edge])
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
            IEnumerable<int> candidatesEnumerable = edges
                .Where(x => x.Contains(prev)) // all neighbors
                .SelectMany(x => x) // flatten
                .Where(x => x != prev); // remove previous


            //The open edges on the previous array
            HashSet<int> openEdgesOnPrevious = geometry.Polygons[prev].GetEdges()
                .Where(x => edges[x].Count == 1) // open edges
                .SelectMany<Edge, int>(e => [e.A, e.B]) // points on edges
                .ToHashSet();

            var orderedCandidates = candidatesEnumerable
                    // Only use boundary polygons
                    .Where(i => priority[i] > -1)
                    // Remove polygons that don't share a point along an open edge.
                    .Where(i => openEdgesOnPrevious.Count == 0 || geometry.Polygons[i].GetEdges().Any(x => openEdgesOnPrevious.Contains(x.A) || openEdgesOnPrevious.Contains(x.B)))
                    //Find the number of open edges.
                    .Select(i => (i, c: geometry.Polygons[i].GetEdges().Count(x => edges[x].Count == 1)))
                    // Order them by descending
                    .OrderByDescending(t => t.c);

            if (prevprev > -1)
            {
                var points = geometry.Polygons[prevprev].Points;

                orderedCandidates = orderedCandidates
                    .ThenBy(i => geometry.Polygons[i.i].Points.Count(p => points.Contains(p)));
                //candidatesEnumerable = candidatesEnumerable
                //    .Where(c => !geometry.Polygons[prevprev].ContainsSharedPoints(geometry.Polygons[c]));
            }

            var candidates = candidatesEnumerable.ToArray();

            //IEnumerable<PolygonIndex> selector  = candidates
            //        // Only use boundary polygons
            //        .Where(i => priority[i] >= -1)
            //        // Remove polygons that don't share a point along an open edge.
            //        .Where(i => openEdgesOnPrevious.Count == 0 || geometry.Polygons[i].GetEdges().Any(x => openEdgesOnPrevious.Contains(x.A) || openEdgesOnPrevious.Contains(x.B)))
            //        //Find the number of open edges.
            //        .Select(i => (i, c: geometry.Polygons[i].GetEdges().Count(x => edges[x].Count == 1)))
            //        // Order them by descending
            //        .OrderByDescending(t => t.c)
            //        // Remove accidental internal ones.
            //        //.Where(t => t.c > 0)
            //        // Only keep the index.
            //        .Select(t => t.i); 

            var selector = orderedCandidates
                .Select(x => x.i);

            var test = selector.ToArray();

            var selected = selector
                .FirstOrDefault(-1);

            if (selected == -1)
                return TryTakeCornerPolygon(prev, prevprev, polygons, edges, priority, geometry);
            return selected;
        }

        /// <summary>
        /// Tries to find a polygon at a corner of a model.
        /// </summary>
        /// <param name="prev"></param>
        /// <param name="prevprev"></param>
        /// <param name="polygons"></param>
        /// <param name="edges"></param>
        /// <param name="priority"></param>
        /// <param name="geometry"></param>
        /// <returns></returns>
        private PolygonIndex TryTakeCornerPolygon(int prev, int prevprev, List<PolygonIndex> polygons, MutableLookup<Edge, PolygonIndex> edges, Dictionary<PolygonIndex, int> priority, IGeometry geometry)
        {

            // Who can be placed.
            IEnumerable<int> candidatesEnumerable = edges
                .Where(x => x.Contains(prev)) // all neighbors
                .SelectMany(x => x) // flatten
                .Where(x => x != prev); // remove previous

            //The open edges on the previous array
            HashSet<int> openPoints = geometry.Polygons[prev].GetEdges()
                .Where(x => edges[x].Count == 1) // open edges
                .SelectMany<Edge, int>(e => [e.A, e.B]) // points on edges
                .ToHashSet();

            if (prevprev > -1)
            {
                var sharedOpens = geometry.Polygons[prevprev].GetEdges()
                    .Where(x => edges[x].Count == 1) // open edges
                    .SelectMany<Edge, int>(e => [e.A, e.B]).ToArray();

                foreach (var p in geometry.Polygons[prev].Points)
                {
                    if (sharedOpens.Contains(p))
                    {
                        openPoints.Add(p);
                    }
                }
            }

            var orderedCandidates = candidatesEnumerable
                    // Only use internal polygons
                    .Where(i => priority[i] == -1)
                    // Remove polygons that don't share a point along an open edge.
                    .Where(i => geometry.Polygons[i].GetEdges().Any(x => openPoints.Contains(x.A) || openPoints.Contains(x.B)))
                    //Find the number of open edges.
                    .Select(i => (i, c: geometry.Polygons[i].GetEdges().Count(x => edges[x].Count == 1)))
                    // Order them by descending
                    .OrderByDescending(t => t.c);

            if (prevprev > -1)
            {
                var points = geometry.Polygons[prevprev].Points;
                orderedCandidates = orderedCandidates
                    .ThenBy(i => geometry.Polygons[i.i].Points.Count(p => points.Contains(p)));
                //candidatesEnumerable = candidatesEnumerable
                //    .Where(c => !geometry.Polygons[prevprev].ContainsSharedPoints(geometry.Polygons[c]));
            }

            var candidates = candidatesEnumerable.ToArray();

            var selector = orderedCandidates
                .Select(x => x.i);

            var test = selector.ToArray();

            return selector
                .FirstOrDefault(-1);
        }
    }

    public class StripSegmentUnroller2(IFlattenedSegmentConstraint[] flattenedGeometryConstraints, bool direction)
    {
        public IFlattenedGeometry[] UnrollSegment(IGeometry geometry, IProgress<double> progress, CancellationToken cancellationToken = default)
        {
            //List of polygons to be placed.
            List<int> polygons = [.. geometry.Polygons.Select((_, i) => i)];

            //lookup of edges and their associated polygons.
            MutableLookup<Edge, PolygonIndex> edges = new(polygons.SelectMany((x, i) => geometry.Polygons[x].GetEdges().Select(y => (y, i))).ToLookup(keySelector: x => x.y, elementSelector: x => x.i));

            Dictionary<PolygonIndex, int> priority = new(polygons.Select((x, i) => KeyValuePair.Create(i, geometry.Polygons[x].GetEdges().Any(x => edges[x].Count == 1) ? 0 : -1)));

            Dictionary<PolygonIndex, int> stripId = [];

            List<IFlattenedGeometry> geos = [];

            while (true)
            {
                var geo = CreatedStrip(polygons, edges, priority, geometry, stripId, cancellationToken);
                if (!geo)
                    break;
                //geos.AddRange(geo);
            }

            if (direction)
            {
                return GetStripsAlong(stripId, geometry);
            }

            return GetStripsAcross(stripId, geometry);
        }

        private IFlattenedGeometry[] GetStripsAlong(Dictionary<PolygonIndex, int> stripId, IGeometry geometry)
        {
            var neighbourSet = geometry.Polygons
                .Select((p, i) 
                    => (
                        polygon: i,
                        neighbors: geometry.Polygons
                            .Select((x, i) => (polygon: x, index: i))
                            .Where(b => b.index != i)
                            .Where((b) => b.polygon.ContainsSharedEdge(p))
                            .Select(b => b.index)
                            .ToArray()
                        )
                    ).ToDictionary(x => x.polygon, x => x.neighbors);

            List<PolygonIndex[]> strips = [];

            while(neighbourSet.Count > 0)
            {
                List<Edge> chain = [];

                List<PolygonIndex> strip = [];

                var p = neighbourSet.First().Key;
                
                List<PolygonIndex> toAddStack = [p];

                while (toAddStack.Count > 0)
                {
                    var poly = toAddStack.Last();
                    toAddStack.RemoveAt(toAddStack.Count - 1);
                    
                    int id = stripId[poly];
                    if (id == 0)
                        continue;

                    var next = neighbourSet[poly].Where(p => stripId[p] == id && stripId[p] > 0).ToArray();

                    foreach(var e in next)
                    {
                        chain.Add(new(poly, e));
                        toAddStack.Add(e);
                    }

                    neighbourSet.Remove(poly);
                    stripId[poly] = 0;
                }

                strips.AddRange(ArrayTransform.CreateEdgeLoops(chain.ToArray()));
            }


            List<IFlattenedGeometry> split = [];


            foreach (var strip in strips)
            {
                FlattenedGeometryBuilder builder = new(geometry, flattenedGeometryConstraints);
                Dictionary<Edge, int> edgeKinds = [];

                int length = strip.First() == strip.Last() 
                    ? strip.Length - 1
                    : strip.Length;

                for (int i = 0; i < length; i++)
                {
                    var polygon = geometry.Polygons[strip[i]];

                    if (!builder.AddPolygon(polygon, out var placed))
                    {
                        if (builder.Polygons.Count > 0)
                            split.Add(ToFlattenedStripGeometry(builder, edgeKinds));
                        builder = new(geometry, flattenedGeometryConstraints);
                        edgeKinds = [];

                        if (!builder.AddPolygon(polygon, out placed))
                            throw new InvalidOperationException("Unable to place initial polygon!");
                    }

                    Edge[] sharedEdges = new Edge[placed.Value.Points.Length];
                    int counter = 0;
                    foreach (var e2 in placed.Value.GetEdges())
                    {
                        // if an edge is already added, mark it as interior(1).
                        if (!edgeKinds.TryAdd(e2, 0))
                        {
                            edgeKinds[e2] = 1;
                            sharedEdges[counter++] = e2;
                        }
                    }

                    for (int j = 0; j < counter; j++)
                    {
                        Edge e2 = sharedEdges[j];

                        //Mark all connecting edges to an interior edge as side edge(2)
                        foreach (var sideEdge in edgeKinds.Keys.Where(e => e != e2 && e.HasSharedPoint(e2)))
                        {
                            edgeKinds[sideEdge] = 2;
                        }
                    }

                }
                if (builder.Polygons.Count > 0)
                    split.Add(ToFlattenedStripGeometry(builder, edgeKinds));

            }

            return [.. split]; 
        }

        private IFlattenedGeometry[] GetStripsAcross(Dictionary<PolygonIndex, int> stripId, IGeometry geometry)
        {
            var neighbourSet = geometry.Polygons
                .Select((p, i)
                    => (
                        polygon: i,
                        neighbors: geometry.Polygons
                            .Select((x, i) => (polygon: x, index: i))
                            .Where(b => b.index != i)
                            .Where((b) => b.polygon.ContainsSharedEdge(p))
                            .Select(b => b.index)
                            .ToArray()
                        )
                    ).ToDictionary(x => x.polygon, x => x.neighbors);

            List<PolygonIndex[]> strips = [];

            while (neighbourSet.Count > 0)
            {
                List<Edge> chain = [];

                List<PolygonIndex> strip = [];

                var p = neighbourSet.First().Key;

                List<PolygonIndex> toAddStack = [p];

                while (toAddStack.Count > 0)
                {
                    var poly = toAddStack.Last();
                    toAddStack.RemoveAt(toAddStack.Count - 1);

                    int id = stripId[poly];
                    if (id == 0)
                        continue;

                    var next = neighbourSet[poly].Where(p => stripId[p] != id && stripId[p] > 0).ToArray();

                    foreach (var e in next)
                    {
                        chain.Add(new(poly, e));
                        toAddStack.Add(e);
                    }

                    neighbourSet.Remove(poly);
                    stripId[poly] = 0;
                }

                strips.AddRange(ArrayTransform.CreateEdgeLoops(chain.ToArray()));
            }


            List<IFlattenedGeometry> split = [];


            foreach (var strip in strips)
            {
                FlattenedGeometryBuilder builder = new(geometry, flattenedGeometryConstraints);
                Dictionary<Edge, int> edgeKinds = [];

                int length = strip.First() == strip.Last()
                    ? strip.Length - 1
                    : strip.Length;

                for (int i = 0; i < length; i++)
                {
                    var polygon = geometry.Polygons[strip[i]];

                    if (!builder.AddPolygon(polygon, out var placed))
                    {
                        if (builder.Polygons.Count > 0)
                            split.Add(ToFlattenedStripGeometry(builder, edgeKinds));
                        builder = new(geometry, flattenedGeometryConstraints);
                        edgeKinds = [];

                        if (!builder.AddPolygon(polygon, out placed))
                            throw new InvalidOperationException("Unable to place initial polygon!");
                    }

                    Edge[] sharedEdges = new Edge[placed.Value.Points.Length];
                    int counter = 0;
                    foreach (var e2 in placed.Value.GetEdges())
                    {
                        // if an edge is already added, mark it as interior(1).
                        if (!edgeKinds.TryAdd(e2, 0))
                        {
                            edgeKinds[e2] = 1;
                            sharedEdges[counter++] = e2;
                        }
                    }

                    for (int j = 0; j < counter; j++)
                    {
                        Edge e2 = sharedEdges[j];

                        //Mark all connecting edges to an interior edge as side edge(2)
                        foreach (var sideEdge in edgeKinds.Keys.Where(e => e != e2 && e.HasSharedPoint(e2)))
                        {
                            edgeKinds[sideEdge] = 2;
                        }
                    }

                }
                if (builder.Polygons.Count > 0)
                    split.Add(ToFlattenedStripGeometry(builder, edgeKinds));

            }

            return [.. split];
        }

        private bool CreatedStrip(List<PolygonIndex> polygons, MutableLookup<Edge, PolygonIndex> edges, Dictionary<PolygonIndex, int> priority, IGeometry geometry, Dictionary<PolygonIndex, int> stripId, CancellationToken cancellationToken = default)
        {

            PolygonIndex a = FindFirstPolygon(polygons, edges, priority);

            if (a == -1)
            {
                return false;
            }

            List<PolygonIndex> added = [];
            int maxPriority = -1;

            added.Add(a);

            maxPriority = Math.Max(maxPriority, priority[a]);

            var b = TakeNextPolygon(a, -1, polygons, edges, priority, geometry);

            while (b > -1)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                added.Add(b);
                
                maxPriority = Math.Max(maxPriority, priority[b]);
                RemovePolygonFromState(a, polygons, edges, priority, geometry);
                int o = a;
                a = b;
                b = TakeNextPolygon(a, o, polygons, edges, priority, geometry);
            }

            RemovePolygonFromState(a, polygons, edges, priority, geometry);

            RemoveAndCleanUp(added, edges, priority, geometry, maxPriority);

            int newStripId = stripId.Count + 1;
            foreach(var aaa in added)
            {
                if (!stripId.TryAdd(aaa, newStripId))
                    throw new InvalidOperationException("Polygon has been added multiple times");
            }

            return true;

            //List<IFlattenedGeometry> split = [];

            //FlattenedGeometryBuilder builder = new(geometry, flattenedGeometryConstraints);
            //Dictionary<Edge, int> edgeKinds = [];

            //for (int i = 0; i < added.Count; i++)
            //{
            //    var polygon = geometry.Polygons[added[i]];

            //    if (!builder.AddPolygon(polygon, out var placed))
            //    {
            //        if (builder.Polygons.Count > 0)
            //            split.Add(ToFlattenedStripGeometry(builder, edgeKinds));
            //        builder = new(geometry, flattenedGeometryConstraints);
            //        edgeKinds = [];

            //        if (!builder.AddPolygon(polygon, out placed))
            //            throw new InvalidOperationException("Unable to place initial polygon!");
            //    }

            //    Edge[] sharedEdges = new Edge[placed.Value.Points.Length];
            //    int counter = 0;
            //    foreach (var e2 in placed.Value.GetEdges())
            //    {
            //        // if an edge is already added, mark it as interior(1).
            //        if (!edgeKinds.TryAdd(e2, 0))
            //        {
            //            edgeKinds[e2] = 1;
            //            sharedEdges[counter++] = e2;
            //        }
            //    }

            //    for (int j = 0; j < counter; j++)
            //    {
            //        Edge e2 = sharedEdges[j];

            //        //Mark all connecting edges to an interior edge as side edge(2)
            //        foreach (var sideEdge in edgeKinds.Keys.Where(e => e != e2 && e.HasSharedPoint(e2)))
            //        {
            //            edgeKinds[sideEdge] = 2;
            //        }
            //    }

            //}
            //if (builder.Polygons.Count > 0)
            //    split.Add(ToFlattenedStripGeometry(builder, edgeKinds));



            //return [.. split];
        }

        private static IFlattenedGeometry ToFlattenedStripGeometry(FlattenedGeometryBuilder builder, Dictionary<Edge, int> edgeKinds)
            => new StripFlattenedGeometry(builder.ToFlattenedGeometry(), edgeKinds);

        private PolygonIndex FindFirstPolygon(List<PolygonIndex> polygons, MutableLookup<Edge, PolygonIndex> edges, Dictionary<PolygonIndex, int> priority)
        {
            return polygons
                .Where(i => priority[i] > -1)
                .OrderBy(i => priority[i])
                .FirstOrDefault(-1);
        }

        private static void RemoveAndCleanUp(List<PolygonIndex> addedPolygons, MutableLookup<Edge, PolygonIndex> edges, Dictionary<PolygonIndex, int> priority, IGeometry geometry, int maxPriority)
        {
            foreach (var added in addedPolygons)
            {
                var poly = geometry.Polygons[added];
                foreach (var edge in poly.GetEdges())
                {
                    foreach (var neighbor in edges[edge])
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

            //The open edges on the previous array
            var openEdgesOnPrevious = geometry.Polygons[prev].GetEdges().Where(x => edges[x].Count == 1).SelectMany<Edge, int>(e => [e.A, e.B]).ToHashSet();
    
            if (prevprev > -1)
            {
                candidatesEnumerable = candidatesEnumerable
                    .Where(c => !geometry.Polygons[prevprev].ContainsSharedPoints(geometry.Polygons[c]));
            }

            var candidates = candidatesEnumerable.ToArray();

            IEnumerable<PolygonIndex> selector  = candidates
                    // Only use boundary polygons
                    .Where(i => priority[i] >= -1)
                    // Remove polygons that don't share a point along an open edge.
                    .Where(i => openEdgesOnPrevious.Count == 0 || geometry.Polygons[i].GetEdges().Any(x => openEdgesOnPrevious.Contains(x.A) || openEdgesOnPrevious.Contains(x.B)))
                    //Find the number of open edges.
                    .Select(i => (i, c: geometry.Polygons[i].GetEdges().Count(x => edges[x].Count == 1)))
                    // Order them by descending
                    .OrderByDescending(t => t.c)
                    // Remove accidental internal ones.
                    //.Where(t => t.c > 0)
                    // Only keep the index.
                    .Select(t => t.i); 

            var test = selector.ToArray();

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

        public StripSegmentUnroller() : this([])
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

            if (!builder.AddPolygon(firstPoly, out _) || !builder.AddPolygon(secondPoly, out _))
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

                if (!builder.AddPolygon(next, out _))
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

            foreach (var firstIndex in goodCandidates)
            {
                Edge firstBest = default;
                Edge secondBest = default;
                double bestDot = treatDirectionAsPlane ? 1.1 : -0.1;

                var first = polygons[firstIndex];
                var mid = geometry.GetMidPoint3d(first);
                var firstEdges = first.GetEdges().ToArray();
                foreach (var a in firstEdges)
                {
                    var aMid = geometry.GetMidPoint3d(a);

                    var fdir = (mid - aMid).Normalized;

                    foreach (var b in firstEdges)
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

            var dir1 = sharedMidpoint - firstMidpoint;
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
