using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using SheetCuttingTools.Abstractions.Behaviors;
using SheetCuttingTools.Behaviors.EdgeFilters;
using System;
using System.Runtime.InteropServices;

namespace SheetCuttingTools.Grasshopper.Components.Behaviors.EdgeFilter;

[Guid("75F6BAAE-EB86-414A-872C-D32E6B678DF0")]
public sealed class IsOfStripTypeEdgeFilterComponent() : BaseEdgeFilterComponent("EF Is of strip type", "IOST", "filters edges based on their strip type. This component only works for flattened geometry produced with the stirp unroller")
{
    protected override IEdgeFilter CreateBehavior(IGH_DataAccess DA)
    {
        GH_Integer kind = new();
        if (!DA.GetData(0, ref kind))
            kind.Value = 0;

        return new IsOfStripTypeEdgeFilter(kind.Value);
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        Param_Integer integer = new();
        integer.Name = "Kind";
        integer.NickName = "K";
        integer.Description = "What strip kind to filter on, default is end";
        integer.Access = GH_ParamAccess.item;
        integer.SetPersistentData(0);
        integer.AddNamedValue("End", 0);
        integer.AddNamedValue("Side", 2);
        integer.AddNamedValue("Interior", 1);
        pManager.AddParameter(integer);
    }
}

