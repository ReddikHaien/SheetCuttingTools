using SheetCuttingTools.Abstractions.Behaviors;
using SheetCuttingTools.Infrastructure.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Behaviors.FlattenedSegmentConstraints
{
    public class FlattenedSegmentIntersectionConstraint : IFlattenedSegmentConstraint
    {
        public string Name()
            => "Behavior/FlattenedSegmentConstraint/IntersectionConstraint";

        public bool ValidateFlatSegment(in FlattenedSegmentCandidate candidate)
        {
            foreach(var edge in candidate.PlacedPolygon.GetEdges())
            {
                if (edge == candidate.AnchorEdge)
                    continue;

                var pa = candidate.GeneratedPoints.First(x => x.Index == edge.A).Point;
                var pb = candidate.GeneratedPoints.First(x => x.Index == edge.B).Point;

                var flattened = candidate.FlattenedPoints;

                var tasks = candidate.Boundary.Chunk(1024).Select(bound => Task.Run(() =>
                {
                    foreach (var (original, placed) in bound)
                    {
                        var pc = flattened[placed.A];
                        var pd = flattened[placed.B];

                        if (GeometryMath.LineOverlap(pa, pb, pc, pd))
                            return false;
                    }
                    return true;
                })).ToArray();

                var result = Task.WhenAll(tasks).Result.All(x => x);
                if (!result)
                    return false;
            }
            return true;
        }
    }
}
