using SheetCuttingTools.Abstractions.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Grasshopper.Components.Behaviors.PolygonScorers
{
    public abstract class BasePolygonScorer(string name, string nickname, string description) : BaseBehaviorComponent<IPolygonScorer>(name, nickname, description)
    {
    }
}
