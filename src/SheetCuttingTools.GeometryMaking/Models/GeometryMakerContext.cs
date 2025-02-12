using SheetCuttingTools.Abstractions.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.GeometryMaking.Models
{
    public class GeometryMakerContext
    {
        private int counter = 1;

        private readonly HashSet<Edge> ProcessedEdges = [];
        private readonly Dictionary<Edge, string> names = [];

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
