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

                foreach(var (original, placed) in candidate.Boundary)
                {
                    var pc = candidate.FlattenedPoints[placed.A];
                    var pd = candidate.FlattenedPoints[placed.B];

                    if (GeometryMath.LineOverlap(pa, pb, pc, pd))
                        return false;
                }
            }

            return true;
        }
    }
}
