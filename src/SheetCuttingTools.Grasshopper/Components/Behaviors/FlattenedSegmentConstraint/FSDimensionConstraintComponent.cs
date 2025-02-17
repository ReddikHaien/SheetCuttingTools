using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using SheetCuttingTools.Abstractions.Behaviors;
using SheetCuttingTools.Behaviors.FlattenedSegmentConstraints;
using System;
using System.Runtime.InteropServices;

namespace SheetCuttingTools.Grasshopper.Components.Behaviors.FlattenedSegmentConstraint
{
    [Guid("8291E49D-68D7-4695-9064-30A8322D3A31")]
    public class FSDimensionConstraintComponent() : BaseFlattenedSegmentConstraint("FS Dimension Constraint", "FSDC", "Checks that a flattened segment is within the specified dimensions")
    {
        protected override IFlattenedSegmentConstraint CreateBehavior(IGH_DataAccess DA)
        {
            GH_Number w = new();
            GH_Number h = new();
            if (!DA.GetData(0, ref w) || !DA.GetData(1, ref h))
            {
                return null!;
            }

            return new FlattenedSegmentDimensionConstraint(w.Value, h.Value);
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Width", "W", "The maximum with of the flattened segment", GH_ParamAccess.item);
            pManager.AddNumberParameter("Height", "H", "The maximum height of the flattened segment", GH_ParamAccess.item);
        }
    }
}
