using SheetCuttingTools.Abstractions.Contracts;
using SheetCuttingTools.Abstractions.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.GeometryMaking.Models
{
    public class GeometryMakerContext(IGeometry geometry)
    {
        private static ConditionalWeakTable<IGeometry, Dictionary<Edge, string>> globalNames = new();

        public static Dictionary<Edge, string> GetNameList(IGeometry geometry)
        {
            var parent = GetOldestParent(geometry);
            lock (globalNames)
            {
                return globalNames.GetOrCreateValue(parent);
            }
        }

        private static IGeometry GetOldestParent(IGeometry geometry)
        {
            while(geometry.Parent is not null)
            {
                geometry = geometry.Parent;
            }
            return geometry;
        }

        private int counter = 1;

        private readonly HashSet<Edge> ProcessedEdges = [];
        private readonly Dictionary<Edge, string> names = GetNameList(geometry);

        public bool EdgeProcessed(Edge edge)
            => ProcessedEdges.Contains(edge);

        public void MarkEdge(Edge edge)
            => ProcessedEdges.Add(edge);

        public string CreateName(Edge edge)
        {
            if (!names.TryGetValue(edge, out var name))
            {
                name = (counter++).ToString("X");
                names[edge] = name;
            }
            return name;
        }
    }
}
