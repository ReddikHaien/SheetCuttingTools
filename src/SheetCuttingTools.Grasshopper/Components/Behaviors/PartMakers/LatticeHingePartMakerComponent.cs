using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using SheetCuttingTools.Abstractions.Behaviors;
using SheetCuttingTools.GeometryMaking.Parts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Grasshopper.Components.Behaviors.PartMakers
{
    [Guid("B6E44B56-AB77-4915-8E86-8A94AEE089C3")]
    public class LatticeHingePartMakerComponent() : BasePartMaker("Lattice Hinge Part Maker", "LHPM", "Part maker for creating lattice hinges")
    {
        protected override IPartMaker CreateBehavior(IGH_DataAccess DA)
        {
            GH_Number n = new();
            if (!DA.GetData(0, ref n))
                n.Value = 3;
            return new LatticeHingePartMaker(n.Value);
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Gap size", "G", "The gap size between two polygons", GH_ParamAccess.item, 3);
        }
    }
}
