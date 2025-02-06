using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using SheetCuttingTools.Abstractions.Behaviors;
using SheetCuttingTools.Behaviors.SegmentConstraints;
using System;
using System.Runtime.InteropServices;

namespace SheetCuttingTools.Grasshopper.Components.Behaviors.SegmentConstraint;

[Guid("ABE2A053-591C-4153-B1A3-D77CA3093EB1")]
public class SegmentDimensionConstraintComponent() : BaseSegmentConstraintComponent("SegmentDimension", "SD", "Prevents segments from growing outside the specified bounds")
{
    protected override ISegmentConstraint CreateBehavior(IGH_DataAccess DA)
    {
        GH_Number length = new();
        GH_Number height = new();
        GH_Number depth = new();

        if (
            !DA.GetData(0, ref length)
          || !DA.GetData(1, ref height)
          || !DA.GetData(2, ref depth))
            throw new InvalidOperationException("Could not fetch data");

        return new SegmentDimensionConstraint(new((float)length.Value, (float)height.Value, (float)depth.Value));
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddNumberParameter("Length", "L", "The maximum size allowed along the X axis", GH_ParamAccess.item);
        pManager.AddNumberParameter("Height", "H", "The maximum size allowed along the y axis", GH_ParamAccess.item);
        pManager.AddNumberParameter("Depth", "D", "The maximum size allowed along the z axis", GH_ParamAccess.item);
    }
}
