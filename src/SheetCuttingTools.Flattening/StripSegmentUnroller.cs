using g3;
using SheetCuttingTools.Abstractions.Behaviors;
using SheetCuttingTools.Abstractions.Contracts;
using SheetCuttingTools.Abstractions.Models;
using SheetCuttingTools.Abstractions.Models.Geometry;
using SheetCuttingTools.Flattening.Builder;
using SheetCuttingTools.Infrastructure.Extensions;
using SheetCuttingTools.Infrastructure.Math;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Flattening
{
    public class StripSegmentUnroller(IFlattenedSegmentConstraint[] flattenedGeometryConstraints)
    {
        private readonly IFlattenedSegmentConstraint[] flattenedGeometryConstraints = flattenedGeometryConstraints;

        public IFlattenedGeometry[] UnrollSegment(IGeometry geometry)
        {
            List<Polygon> polygons = [.. geometry.Polygons];

            List<IFlattenedGeometry> strips = [];

            while(polygons.Count > 0)
            {
                var strip = CreateStrip(polygons, geometry);
                if (strip is null)
                    break;
                strips.Add(strip);
            }


            return [.. strips];
        }

        private IFlattenedGeometry CreateStrip(List<Polygon> polygons, IGeometry geometry)
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

            var sharedEdge = firstPoly.GetEdges().Intersect(secondPoly.GetEdges()).First();

            List<Polygon> strip = [firstPoly, secondPoly];
            List<int> indexStrip = [first, second];
            edges = polygons.SelectMany((x, i) => x.GetEdges().Select(y => (y, i))).ToLookup(keySelector: x => x.y, elementSelector: x => x.i);
            while (true)
            {
                var opposite = strip[^1].GetEdges().First(x => !x.ContainsPoint(sharedEdge.A) && !x.ContainsPoint(sharedEdge.B));

                candidates = edges[opposite].ToArray();
                if (candidates.Length == 0)
                    break;

                var next = polygons[candidates[0]];

                if (!builder.AddPolygon(next))
                    break;

                strip.Add(next);
                polygons.RemoveAt(candidates[0]);
                edges = polygons.SelectMany((x, i) => x.GetEdges().Select(y => (y, i))).ToLookup(keySelector: x => x.y, elementSelector: x => x.i);
                sharedEdge = next.GetEdges().Intersect(strip[^2].GetEdges()).First();
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

            foreach(var (p, i) in polys)
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
    }
}
