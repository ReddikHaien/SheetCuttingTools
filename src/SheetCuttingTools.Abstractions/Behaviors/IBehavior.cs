using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Abstractions.Behaviors
{
    /// <summary>
    /// Marker interface for behaviors
    /// </summary>
    public interface IBehavior
    {
        public const string RootName = "Behavior";

        string Name();
    }
}
