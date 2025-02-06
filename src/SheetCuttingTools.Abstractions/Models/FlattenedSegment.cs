using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Abstractions.Models
{
    public class FlattenedSegment
    {
        public Segment Segment { get; set; } = null!;

        public Vector2[] Points { get; set; } = [];

        public (Polygon Original, Polygon Placed)[] Polygons { get; set; } = [];

        public (Vector2, Vector2) GetEdge(Edge edge)
        => (Points[edge.A], Points[edge.B]);
    }
}
