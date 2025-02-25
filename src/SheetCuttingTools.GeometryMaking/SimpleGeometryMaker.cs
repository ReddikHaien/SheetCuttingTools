using g3;
using SheetCuttingTools.Abstractions.Contracts;
using SheetCuttingTools.Abstractions.Models;
using SheetCuttingTools.GeometryMaking.Models;
using SheetCuttingTools.Infrastructure.Extensions;
using SheetCuttingTools.Infrastructure.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SheetCuttingTools.GeometryMaking.PaperGeometryMaker;

namespace SheetCuttingTools.GeometryMaking
{
    public class SimpleGeometryMaker
    {
        public const string Side = "Geometry.Side";
        public const string Hinge = "Geometry.Fold";
        public const string Label = "Geometry.Label";

        public Sheet CreateSheet(IFlattenedGeometry segment, GeometryMakerContext context)
        {

            Dictionary<Edge, EdgeKind> edges = [];
            Dictionary<Edge, string> names = [];

            var lookup = segment.PlacedPolygons.SelectMany(x => x.Placed.GetEdges().Zip(x.Original.GetEdges())).ToLookup(x => x.Second, x => x.First);


            foreach (var group in lookup)
            {
                var original = group.Key;
                foreach (var edge in group)
                {
                    if (edges.ContainsKey(edge))
                    {
                        edges[edge] = EdgeKind.Hinge;
                        continue;
                    }

                    

                    edges[edge] = EdgeKind.Side;
                    context.MarkEdge(original);
                }
            }

            List<int[]> boundaryLines = [];

            List<(Edge placed, Edge original)> stack = segment.PlacedPolygons.SelectMany(x => x.Placed.GetEdges().Zip(x.Original.GetEdges())).Where(x => edges[x.First] is EdgeKind.Side).ToList();

            while (stack.Count > 0)
            {
                List<int> line = [];
                bool found = true;
                while (found)
                {
                    found = false;
                    for (int i = 0; i < stack.Count; i++)
                    {
                        (Edge e, Edge original) = stack[i];
                        if (line.Count == 0)
                        {
                            found = true;
                            line.Add(e.A);
                            line.Add(e.B);
                        }
                        else if (line[^1] == e.A)
                        {
                            found = true;
                            line.Add(e.B);
                        }
                        else if (line[^1] == e.B)
                        {
                            line.Add(e.A);
                            found = true;
                        }
                        else if (line[0] == e.A)
                        {
                            found = true;
                            line.Insert(0, e.B);
                        }
                        else if (line[0] == e.B)
                        {
                            line.Insert(0, e.A);
                            found = true;
                        }
                        else
                        {
                            continue;
                        }

                        names.Add(e, context.CreateName(original));

                        stack.RemoveAt(i);
                        i--;
                    }
                }
                boundaryLines.Add([.. line]);
            }

            List<Vector2d[]> generatedBoundaryLines = [];

            foreach (var line in boundaryLines)
            {

                List<Vector2d> boundaryLine = [];
                foreach (var (a, b) in line.SlidingWindow())
                {
                    if (a == b)
                        continue;

                    var edge = new Edge(a, b);
                    var (pa, pb) = segment.GetPoints(edge);

                    if (boundaryLine.Count == 0)
                    {
                        boundaryLine.Add(pa);
                    }

                    boundaryLine.Add(pb);
                }

                generatedBoundaryLines.Add([.. boundaryLine]);
            }

            List<Vector2d[]> foldLines = [];

            foreach (var edge in edges.Where(x => x.Value is EdgeKind.Hinge))
            {
                var (pa, pb) = segment.GetPoints(edge.Key);

                foldLines.Add([pa, pb]);
            }

            return new Sheet()
            {
                FlattenedSegment = segment,
                Lines = generatedBoundaryLines
                    .Select(x => (Key: Side, Value: x))
                    .Concat(foldLines.Select(x => (Key: Hinge, Value: x)))
                .ToLookup(x => x.Key, x => x.Value),
                BoundaryNames = names
            };

        }
            internal enum EdgeKind
            {
                Hinge,
                Side,
            }

    }
}
