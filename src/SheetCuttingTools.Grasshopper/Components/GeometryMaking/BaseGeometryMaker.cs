using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Grasshopper.Components.GeometryMaking
{
    public abstract class BaseGeometryMaker(string name, string nickname, string description) : BaseToolComponent(name, nickname, description, Constants.Category, Constants.GeometryMakingCategory)
    {
    }
}
