using SheetCuttingTools.Abstractions.Behaviors;

namespace SheetCuttingTools.Grasshopper.Components.Behaviors.FlattenedSegmentConstraint;

public abstract class BaseFlattenedSegmentConstraint(string name, string nickname, string description) : BaseBehaviorComponent<IFlattenedSegmentConstraint>(name, nickname, description)
{
}
