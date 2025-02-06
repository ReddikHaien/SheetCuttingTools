using SheetCuttingTools.Abstractions.Behaviors;

namespace SheetCuttingTools.Grasshopper.Components.Behaviors.SegmentConstraint;

public abstract class BaseSegmentConstraintComponent(string name, string nickname, string description) : BaseBehaviorComponent<ISegmentConstraint>(name, nickname, description)
{
}
