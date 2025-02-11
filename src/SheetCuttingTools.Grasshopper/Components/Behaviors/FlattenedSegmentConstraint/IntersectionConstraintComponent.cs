using Grasshopper.Kernel;
using SheetCuttingTools.Abstractions.Behaviors;
using SheetCuttingTools.Behaviors.FlattenedSegmentConstraints;
using System;
using System.Runtime.InteropServices;

namespace SheetCuttingTools.Grasshopper.Components.Behaviors.FlattenedSegmentConstraint;

[Guid("B77DF690-1A3B-4347-A8A8-5A6CCBE111F3")]
public class IntersectionConstraintComponent() : BaseFlattenedSegmentConstraint("Flattened Segment Intersection Constraint", "FSIC", "Prevents flattened segments from self intersecting")
{
    protected override IFlattenedSegmentConstraint CreateBehavior(IGH_DataAccess DA)
        => new FlattenedSegmentIntersectionConstraint();

    protected override void RegisterInputParams(GH_InputParamManager pManager){}
}
