using Grasshopper.Kernel;
using SheetCuttingTools.Abstractions.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Grasshopper.Components.Behaviors.EdgeFilter
{
    public abstract class BaseEdgeFilterComponent(string name, string nickname, string description) : BaseBehaviorComponent<IEdgeFilter>(name, nickname, description)
    {
    }
}
