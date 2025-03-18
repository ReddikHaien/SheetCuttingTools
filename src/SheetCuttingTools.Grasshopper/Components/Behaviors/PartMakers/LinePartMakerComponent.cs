using Grasshopper.Documentation;
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
    [Guid("ED82849E-7DF6-458D-AE5F-698808A5EC11")]
    public class LinePartMakerComponent() : BasePartMaker("Line Part Maker", "LPM", "Creates a simple line as the geometry.")
    {
        protected override IPartMaker CreateBehavior(IGH_DataAccess DA)
        {
            GH_Number gap = new();
            GH_Number reduce = new();
            GH_String category = new();
            if (!DA.GetData(0, ref gap))
                gap.Value = 0;

            if (!DA.GetData(1, ref reduce))
                reduce.Value = 0;

            if (!DA.GetData(2, ref category))
                category.Value = "Line";

            return new LinePartMaker(gap.Value, reduce.Value, category.Value);
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Gap size", "G", "The gap between polygons", GH_ParamAccess.item, 0);
            pManager.AddNumberParameter("Line reduction", "R", "How much to shrink a line a line along its own vector", GH_ParamAccess.item, 0);
            pManager.AddTextParameter("Category", "C", "The category to place the line in", GH_ParamAccess.item, "Line");
        }
    }
}
