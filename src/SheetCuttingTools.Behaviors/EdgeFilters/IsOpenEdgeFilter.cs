using SheetCuttingTools.Abstractions.Behaviors;
using SheetCuttingTools.Abstractions.Contracts;
using SheetCuttingTools.Abstractions.Models;
using SheetCuttingTools.Flattening.Models;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Behaviors.EdgeFilters
{
    
    public class EdgeTypeBaseFilter
    {
        protected static ConditionalWeakTable<IFlattenedGeometry, Dictionary<Edge, int>> flattenedGeometries = [];
        protected static ConditionalWeakTable<IGeometry, Dictionary<Edge, int>> geometries = [];

        protected static Dictionary<Edge, int> GetDictionaryForGeometry(IGeometry geometry)
        {
            var dict = geometries.GetOrCreateValue(geometry);
            if (dict.Count == 0)
            {
                lock (dict)
                {
                    if (dict.Count == 0)
                    {

                        var edges = geometry.Polygons
                            .SelectMany(x => x.GetEdges())
                            .GroupBy(x => x);

                        foreach (var g in edges)
                        {
                            dict[g.Key] = g.Count();
                        }
                    }
                }
            }

            return dict;
        }

        protected static Dictionary<Edge, int> GetDictionaryForFlattened(IFlattenedGeometry flattenedGeometry)
        {
            var dict = flattenedGeometries.GetOrCreateValue(flattenedGeometry);
            if (dict.Count == 0)
            {
                lock (dict)
                {
                    if (dict.Count == 0)
                    {

                        var edges = flattenedGeometry.PlacedPolygons
                            .Select(x => x.Placed)
                            .SelectMany(x => x.GetEdges())
                            .GroupBy(x => x);

                        foreach(var g in edges)
                        {
                            dict[g.Key] = g.Count();
                        }
                    }
                }
            }

            return dict;
        }
    }

    /// <summary>
    /// An edge filter that checks if an edge is open(e.g. naked)
    /// </summary>
    /// <param name="asFlattened">Wether to check the geometry based on it being flattened.</param>
    public class IsOpenEdgeFilter(bool asFlattened) : EdgeTypeBaseFilter, IEdgeFilter
    {
        public bool FilterEdge(in EdgeFilterCandidate candidate)
        {
            Edge edge = default;
            Dictionary<Edge, int> dict = null!;
            if (!asFlattened)
            {
                edge = candidate.Edge;
                dict = GetDictionaryForGeometry(candidate.Model);
            }
            else
            {
                if (!candidate.FlattenedEdge.HasValue)
                    return false;

                if (candidate.Model is not IFlattenedGeometry geometry)
                    return false;

                edge = candidate.FlattenedEdge.Value;
                dict = GetDictionaryForFlattened(geometry);

            }


            return dict[edge] == 1;
        }


        public string Name()
            => $"{IEdgeFilter.RootName}/IsOpenEdge";
    }
}
