using SheetCuttingTools.Abstractions.Models;
using SheetCuttingTools.GeometryMaking.Models;
using SheetCuttingTools.Infrastructure.Extensions;
using SheetCuttingTools.Infrastructure.Math;
using System.Numerics;

namespace SheetCuttingTools.GeometryMaking
{
    public class PaperGeometryMaker
    {
        public const string Boundary = "Paper.Boundary";
        public const string ValleyFold = "Paper.ValleyFold";
        public const string MountainFold = "Paper.MountainFold";
        public const string GlueTap = "Paper.GlueTap";

        public Sheet CreateSheet(FlattenedSegment segment, GeometryMakerContext context)
        {
            //HashSet<Edge> tapped = [];
            Dictionary<Edge, Kind> edges = [];
            Dictionary<Edge, string> names = [];

            var lookip = segment.Polygons.SelectMany(x => x.Placed.GetEdges().Zip(x.Original.GetEdges())).ToLookup(x => x.Second, x => x.First);

            

            foreach(var group in lookip)
            {
                var original = group.Key;
                foreach(var edge in group)
                {
                    if (edges.ContainsKey(edge))
                    {
                        edges[edge] = Kind.Fold;
                        continue;
                    }

                    if (context.EdgeProcessed(original))
                    {
                        edges[edge] = Kind.Cut;
                        continue;
                    }

                    edges[edge] = Kind.Tap;
                    context.MarkEdge(original);
                }
            }

            //foreach (var (original, placed) in segment.Polygons)
            //{
            //    foreach (var (pedge, oedge) in placed.GetEdges().Zip(original.GetEdges()))
            //    {
            //        // It's already placed, mark it as an fold
            //        if (edges.ContainsKey(pedge))
            //        {
            //            edges[pedge] = Kind.Fold;
            //            continue;
            //        }

            //        //if (tapped.Contains(oedge)){
            //        //    edges[pedge] = Kind.Cut;
            //        //    continue;
            //        //}

            //        edges[pedge] = Kind.Tap;
            //        tapped.Add(oedge);
            //    }
            //}

            List<int[]> boundaryLines = [];

            List<(Edge placed, Edge original)> stack = segment.Polygons.SelectMany(x => x.Placed.GetEdges().Zip(x.Original.GetEdges())).Where(x => edges[x.First] is Kind.Tap or Kind.Cut).ToList();

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

            List<Vector2[]> generatedBoundaryLines = [];

            foreach(var line in boundaryLines)
            {
               
                List<Vector2> boundaryLine = [];
                List<(Edge, int)> taps = [];
                foreach(var (a, b) in line.SlidingWindow())
                {
                    if (a == b)
                        continue;

                    var edge = new Edge(a, b);
                    var (pa, pb) = segment.GetEdge(edge);
                   
                    if (boundaryLine.Count == 0)
                    {
                        boundaryLine.Add(pa);
                    }
                  
                    if (edges.TryGetValue(edge, out Kind value) && value is Kind.Tap)
                    {
                        taps.Add((edge, boundaryLine.Count));
                    }
                    else if (value is Kind.Fold)
                    {
                        continue;
                    }

                    boundaryLine.Add(pb);
                }

                //Work with the taps in reversse order to preserve their indices
                taps.Reverse();

                foreach(var (edge, index) in taps)
                {
                    var (pa, pb) = segment.GetEdge(edge);
                    
                    var u = pb - pa;
                    var v = segment.Normals[edge];

                    var p2 = v * 3 + u * 0.25f;
                    var p3 = v * 3 + u * 0.75f;

                    bool overlap = false;
                    for (int i = 0; i < boundaryLine.Count - 1; i++)
                    {
                        int j = i + 1;
                        var pc = boundaryLine[i];
                        var pd = boundaryLine[j];

                        if (GeometryMath.LineOverlap(pa, p2 + pa, pc, pd)
                            || GeometryMath.LineOverlap(p2 + pa, p3 + pa, pc, pd)
                            || GeometryMath.LineOverlap(p3 + pa, pb, pc, pd))
                        {
                            overlap = true;
                            break;
                        }
                    }

                    if (overlap)
                    {
                        edges[edge] = Kind.Cut;
                    }
                    else
                    {
                        boundaryLine.InsertRange(index, [p2 + pa, p3 + pa]);
                    }
                }

                generatedBoundaryLines.Add([.. boundaryLine]);
            }

            List<Vector2[]> foldLines = [];

            foreach(var edge in edges.Where(x => x.Value is Kind.Fold or Kind.Tap))
            {
                var (pa, pb) = segment.GetEdge(edge.Key);

                foldLines.Add([pa, pb]);
            }

            return new Sheet()
            {
                FlattenedSegment = segment,
                Lines = generatedBoundaryLines
                    .Select(x => (Key: GlueTap, Value: x))
                    .Concat(foldLines.Select(x => (Key: ValleyFold, Value: x)))
                .ToLookup(x => x.Key, x => x.Value),
                BoundaryNames = names
            };
        }

        public enum Kind
        {
            Fold,
            Cut,
            Tap
        }

    }
}
