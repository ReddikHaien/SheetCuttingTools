using g3;
using SheetCuttingTools.Abstractions.Behaviors;
using SheetCuttingTools.Abstractions.Contracts;
using SheetCuttingTools.Abstractions.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.GeometryMaking.Parts
{
    public class CustomPartMaker(double gap, Action<IPartMakerContext, IPartGeometryOutput> maker) : IPartMaker
    {
        private readonly double gap = gap;
        private readonly Action<IPartMakerContext, IPartGeometryOutput> maker = maker;

        public void CreatePart(IPartMakerContext context, IPartGeometryOutput geometryOutput)
            => maker(context, geometryOutput);

        public double GetRequiredGap(IPartMakerContext partMakerContext)
            => gap;

        public string Name()
            => $"{IPartMaker.RootName}/Custom";
    }
}
