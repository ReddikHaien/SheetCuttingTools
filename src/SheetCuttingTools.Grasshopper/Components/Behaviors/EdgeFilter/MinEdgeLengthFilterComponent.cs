using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using SheetCuttingTools.Abstractions.Behaviors;
using SheetCuttingTools.Behaviors.EdgeFilters;
using System;
using System.Runtime.InteropServices;

namespace SheetCuttingTools.Grasshopper.Components.Behaviors.EdgeFilter
{
    [Guid("08EC7608-5CA8-4963-A734-C9CDB5739D53")]
    public class MinEdgeLengthFilterComponent() : BaseEdgeFilterComponent("MinEdgeLength", "MEL", "Filters out edges that do not have the required minimum length")
    {
        protected override IEdgeFilter CreateBehavior(IGH_DataAccess DA)
        {
            GH_Number minEdgeLengh = new();
            if (!DA.GetData(0, ref minEdgeLengh))
                throw new InvalidOperationException("Failed to get input value");

            return new MinEdgeLengthFilter((float)minEdgeLengh.Value);
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
            => pManager.AddNumberParameter("MinEdgeLength", "M", "The minimum required edge length", GH_ParamAccess.item);
    }
}
