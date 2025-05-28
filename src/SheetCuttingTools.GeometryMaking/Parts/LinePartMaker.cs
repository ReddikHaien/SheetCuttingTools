using SheetCuttingTools.Abstractions.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.GeometryMaking.Parts
{
    public class LinePartMaker(double gap, double reduce, string category = "Line") : IPartMaker
    {
        public void CreatePart(IPartMakerContext context, IPartGeometryOutput geometryOutput)
        {
            var v = (context.A - context.B).Normalized * reduce;


            // prevent double lines.
            geometryOutput.AddLine([context.A - v, context.B + v], category: category);
        }

        public double GetRequiredGap(IPartMakerContext partMakerContext)
            => gap;

        public string Name()
            => $"{IPartMaker.RootName}/Line";
    }
}
