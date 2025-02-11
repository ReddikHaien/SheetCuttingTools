using SheetCuttingTools.Abstractions.Models;
using SheetCuttingTools.Infrastructure.Extensions;
using System.Numerics;

namespace SheetCuttingTools.GeometryMaking
{
    public class PaperGeometryMaker
    {
        public const string Boundary = "Paper.Boundary";
        public const string ValleyFold = "Paper.ValleyFold";
        public const string MountainFold = "Paper.MountainFold";
        public const string GlueTap = "Paper.GlueTap";

        public Sheet CreateSheet(FlattenedSegment segment)
        {
            HashSet<Edge> tapped = [];
            Dictionary<Edge, Kind> edges = [];

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

                    if (tapped.Contains(original))
                    {
                        edges[edge] = Kind.Cut;
                        continue;
                    }

                    edges[edge] = Kind.Tap;
                    tapped.Add(original);
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

            List<Edge> stack = segment.Polygons.SelectMany(x => x.Placed.GetEdges()).Where(x => edges[x] is Kind.Tap or Kind.Cut).Distinct().ToList();

            var x = segment.Polygons[0].Placed.GetEdges().ToArray();

            while (stack.Count > 0)
            {
                List<int> line = [];
                bool found = true;
                while (found)
                {
                    found = false;
                    for (int i = 0; i < stack.Count; i++)
                    {
                        Edge e = stack[i];
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
                foreach(var (a, b) in line.SlidingWindow())
                {
                    var edge = new Edge(a, b);
                    var (pa, pb) = segment.GetEdge(edge);
                   
                    if (boundaryLine.Count == 0)
                    {
                        boundaryLine.Add(pa);
                    }
                  
                    if (!edges.TryGetValue(edge, out Kind value) || value is Kind.Cut)
                    {
                        boundaryLine.Add(pb);
                        continue;
                    }
                    if (edges[edge] is Kind.Tap)
                    {
                        var u = pb - pa;
                        var v = segment.Normals[edge];

                        var p2 = v * 3 + u * 0.16f;
                        var p3 = v * 3 + u * 0.84f;

                        boundaryLine.AddRange([p2 + pa, p3 + pa, pb]);
                    }
                }

                if (line[0] == line[^1])
                    boundaryLine.RemoveAt(boundaryLine.Count - 1);

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
                .ToLookup(x => x.Key, x => x.Value)
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
