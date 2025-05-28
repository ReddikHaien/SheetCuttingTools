using SheetCuttingTools.Abstractions.Behaviors;
using SheetCuttingTools.Grasshopper.Components.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Grasshopper.Components.Behaviors.PartMakers
{
    public abstract class BasePartMaker(string name, string nickname, string description) : BaseBehaviorComponent<IPartMaker>(name, nickname, description)
    {
    }
}
