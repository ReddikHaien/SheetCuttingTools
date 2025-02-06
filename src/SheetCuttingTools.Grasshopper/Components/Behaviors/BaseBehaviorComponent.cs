using Grasshopper.Kernel;
using SheetCuttingTools.Abstractions.Behaviors;
using System;

namespace SheetCuttingTools.Grasshopper.Components.Behaviors;

public abstract class BaseBehaviorComponent<TBehavior>(string name, string nickname, string description) : GH_Component(name, nickname, description, Constants.Category, Constants.BehaviorCategory)
    where TBehavior : IBehavior
{
    public override Guid ComponentGuid => GetType().GUID;

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        => pManager.AddGenericParameter("Behavior", "B", $"A behavior of type {typeof(TBehavior).Name}", GH_ParamAccess.item);

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        try
        {
            var solver = CreateBehavior(DA);
            DA.SetData(0, solver);
        }
        catch (Exception ex)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Failed to create behavior: {ex.Message}");
        }
    }

    protected abstract TBehavior CreateBehavior(IGH_DataAccess DA);
}
