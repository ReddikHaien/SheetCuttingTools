using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using SheetCuttingTools.Abstractions.Behaviors;
using SheetCuttingTools.Behaviors.EdgeFilters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Grasshopper.Components.Behaviors.EdgeFilter
{
    [Guid("1AFBE311-B0B5-482A-9CF8-15EEFE19A79F")]
    public class IsOpenEdgeFilterComponent() : BaseEdgeFilterComponent("EF Is Open Edge", "EFIOE", "Checks if an edge is open or not (e.g. naked)")
    {
        protected override IEdgeFilter CreateBehavior(IGH_DataAccess DA)
        {
            GH_Boolean asFlattened = new();
            if (!DA.GetData(0, ref asFlattened))
            {
                asFlattened.Value = true;
            }
            return new IsOpenEdgeFilter(asFlattened.Value);
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("As flattened", "F", "Wether to treat the edges as flattened", GH_ParamAccess.item, true);
        }
    }
}
