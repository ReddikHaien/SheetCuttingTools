using g3;
using SheetCuttingTools.Abstractions.Behaviors;
using SheetCuttingTools.Abstractions.Contracts;
using SheetCuttingTools.Abstractions.Models;
using SheetCuttingTools.Infrastructure.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.GeometryMaking.Parts
{
    public class LatticeHingePartMaker(double gapSize) : IPartMaker
    {
        private const string name = $"{IPartMaker.RootName}/LatticeHinge";

        private readonly double gapSize = gapSize;

        private readonly double hingeWidthPercentage = 0.1;

        private readonly double rodWidth = 0.5;

        public double GetRequiredGap(IPartMakerContext ctx)
            => gapSize;

        public void CreatePart(IPartMakerContext context, IPartGeometryOutput geometryOutput)
        {
            // oa                          ob
            // |                           | rw
            // z---------------------w     |
            //                       |     | gs - rw
            // a---------------------q     b

            var oa = context.OriginalA;
            var ob = context.OriginalB;
            var a = context.A;
            var b = context.B;
            var ab = context.U;

            var normal = context.V;

            var l = a.Distance(b);
            
            var q = a + ab * l * (1 - hingeWidthPercentage);
            var w = q + normal * (gapSize - rodWidth);

            var ca = a + normal * gapSize;
            var z = ca - normal * rodWidth;

            var cb = b + normal * gapSize;

            geometryOutput.AddLine([a, q, w, z, oa]);
            geometryOutput.AddLine([b, ob]);
        }

        public string Name() => name;
    }
}
