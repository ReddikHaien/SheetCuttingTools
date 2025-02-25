using g3;
using SheetCuttingTools.Abstractions.Contracts;
using SheetCuttingTools.Abstractions.Models;
using SheetCuttingTools.GeometryMaking.Models;
using SheetCuttingTools.GeometryMaking.Parts;
using SheetCuttingTools.Infrastructure.Extensions;
using SheetCuttingTools.Infrastructure.Math;

namespace SheetCuttingTools.GeometryMaking
{
    /// <summary>
    /// Geometry maker that is based on parts
    /// </summary>
    /// <param name="hingeMaker">The part maker for hinged edges.</param>
    /// <param name="connectionMaker">The part maker for connection edges.</param>
    public class PartGeometryMaker(IPartMaker hingeMaker, IPartMaker connectionMaker)
    {

        private readonly IPartMaker hingeMaker = hingeMaker;
        private readonly IPartMaker connectionMaker = connectionMaker;

        public Sheet CreateSheet(IFlattenedGeometry segment, GeometryMakerContext context)
        {
            Dictionary<Edge, EdgeKind> kinds = [];
            Dictionary<Edge, string> names = [];
            var lookup = segment.PlacedPolygons.SelectMany(x => x.Placed.GetEdges().Zip(x.Original.GetEdges())).ToLookup(x => x.Second, x => x.First);

            foreach (var group in lookup)
            {
                var original = group.Key;
                foreach (var edge in group)
                {
                    if (kinds.ContainsKey(edge))
                    {
                        kinds[edge] = EdgeKind.Hinge;
                        continue;
                    }

                    if (context.EdgeProcessed(original))
                    {
                        kinds[edge] = EdgeKind.ConnectionMale;
                        continue;
                    }

                    kinds[edge] = EdgeKind.ConnectionFemale;
                    context.MarkEdge(original);
                }
            }

            PartMakerContext partContext = new(false);

            HashSet<Edge> edges = [];
            List<Vector2d> points = [];

            foreach ((Polygon Original, Polygon placed) in segment.PlacedPolygons)
            {
                int l = placed.Points.Length;
                var mid = segment.GetMidPoint2d(placed);

                (Edge, Vector2d)[] normals = new (Edge, Vector2d)[l];
                foreach (var (edge, i) in placed.GetEdges().Select((x, i) => (x, i)))
                {
                    var (a, b) = segment.GetPoints(edge);

                    double distance = kinds[edge] is EdgeKind.Hinge
                        ? hingeMaker.GetRequiredGap(true)
                        : hingeMaker.GetRequiredGap(kinds[edge] is EdgeKind.ConnectionMale);

                    normals[i] = (edge, GeometryMath.NormalToLine(mid, a, b) * distance);
                }

                Vector2d[] newPoints = new Vector2d[l];

                for (int i = 0; i < l; i++)
                {
                    int j = (i + l - 1) % l;

                    var (iEdge, iNormal) = normals[i];
                    var (jEdge, jNormal) = normals[j];

                    var (ia, ib) = segment.GetPoints(iEdge);
                    var (ja, jb) = segment.GetPoints(jEdge);

                    newPoints[i] = GeometryMath.LineIntersection(ia + iNormal, ib + iNormal, ja + jNormal, jb + jNormal);
                }

                for (int i = 0; i < l; i++)
                {
                    int j = (i + 1) % l;

                    Vector2d pb = newPoints[i];
                    Vector2d pa = newPoints[j];

                    (Edge edge, Vector2d normal) = normals[i];

                    PartMakerContext c = new(kinds[edge] is EdgeKind.ConnectionMale);

                    
                    if (kinds[edge] is EdgeKind.Hinge)
                    {
                        hingeMaker.CreatePart(edge, pa, pb, -normal.Normalized, segment, c);
                    }
                    else
                    {
                        connectionMaker.CreatePart(edge, pa, pb, -normal, segment, c);
                        names.TryAdd(edge, context.CreateName(edge));
                    }
                    partContext.AddContext(c);
                }
            }

            var loops = ArrayTransform.CreateEdgeLoops(edges.ToArray());

            List<(string, Vector2d[])> lines = [];

            foreach (var loop in loops)
            {
                var line = loop.Select(x => points[x]).ToArray();

                lines.Add(("Geometry", line));

            }

            return new Sheet
            {
                FlattenedSegment = segment,
                Lines = lines.ToLookup(keySelector: x => x.Item1, elementSelector: x => x.Item2),
                BoundaryNames = names.AsReadOnly(),
            };

        }

        public enum EdgeKind
        {
            Hinge,
            ConnectionMale,
            ConnectionFemale,
        }
    }
}
