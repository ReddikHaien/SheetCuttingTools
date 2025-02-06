using SheetCuttingTools.Abstractions.Behaviors;
using SheetCuttingTools.Abstractions.Contracts;
using SheetCuttingTools.Abstractions.Models;
using SheetCuttingTools.Segmentation.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Segmentation
{
    /// <summary>
    /// This segmentation class tries to divide the input model in a "first come first serve" fashion.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This segmentor will greedily fill a segment before moving on to the next one. This can result in large sections with small sections between.
    /// </para>
    /// </remarks>
    public class GreedySegmentator(IPolygonScorer[] polygonScorers, IEdgeFilter[] edgeFilters, ISegmentConstraint[] segmentConstraints)
    {
        public IPolygonScorer[] PolygonScorers { get; } = polygonScorers;
        public IEdgeFilter[] EdgeFilters { get; } = edgeFilters;
        public ISegmentConstraint[] SegmentConstraints { get; } = segmentConstraints;

        public Segment[] SegmentateModel(IGeometryProvider model)
        {
            if (model.Polygons.Count == 0)
                return [];


            List<Polygon> polygons = [.. model.Polygons];
            var neighbours = model.Polygons
                    .SelectMany(x => x.GetEdges().Select(y => (edge: y, poly: x)))
                    .ToLookup(x => x.edge, y => y.poly)
                    .ToDictionary(x => x.Key, x => x.ToList());

            List<SegmentBuilder> segments = [];


            segments.Add(CreateNewBuilder(polygons, neighbours, model));

            while (polygons.Count > 0)
            {
                bool found = false;
                var segmentBuilder = segments[^1];
                var edges = segmentBuilder.Boundary
                    .Where(e => EdgeFilters.Length == 0 || EdgeFilters.All(x => x.FilterEdge(new(e, model))))
                    .ToArray();

                foreach( var e in edges)
                {
                    if (!neighbours.TryGetValue(e, out var polys))
                        continue;

                    var opposite = polys[0];

                    if (segmentBuilder.AddPolygon(opposite, e))
                    {
                        polygons.Remove(opposite);
                        RemoveNeighbor(neighbours, opposite);
                        found = true;
                    }
                }

                if (!found)
                {
                    segments.Add(CreateNewBuilder(polygons, neighbours, model));
                }
            }

            return segments.Select(x => x.ToSegment(model as Segment)).ToArray();
        }

        private SegmentBuilder CreateNewBuilder(List<Polygon> polygons, Dictionary<Edge, List<Polygon>> neighbors, IGeometryProvider geometry)
        {
            var polygon = PolygonScorers.Length > 0
                ? polygons.MaxBy(p => PolygonScorers.Average(x => x.ScorePolygon(new(p, geometry))))
                : polygons.First();

            var builder = new SegmentBuilder(geometry, SegmentConstraints);
            builder.AddPolygon(polygon, polygon.GetEdges().First());

            polygons.Remove(polygon);
            RemoveNeighbor(neighbors, polygon);

            return builder;
        }

        private static void RemoveNeighbor(Dictionary<Edge, List<Polygon>> neighbors, Polygon polygon)
        {
            foreach (var edge in polygon.GetEdges())
            {
                var n = neighbors[edge];
                n.Remove(polygon);
                if (n.Count == 0)
                {
                    neighbors.Remove(edge);
                    continue;
                }

                neighbors[edge] = n;
            }
        }
    }
}
