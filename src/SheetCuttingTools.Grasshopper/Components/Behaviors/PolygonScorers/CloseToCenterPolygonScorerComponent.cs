using Grasshopper.Kernel;
using SheetCuttingTools.Abstractions.Behaviors;
using SheetCuttingTools.Behaviors.PolygonScorers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Grasshopper.Components.Behaviors.PolygonScorers
{
    [Guid("BBBF1AF7-E514-4CAF-BE75-8392C66F2F05")]
    public class CloseToCenterPolygonScorerComponent() : BasePolygonScorer("Close to center polygon scorer", "CTC", "Scores polygons based on how close they are to the geometry's center.")
    {
        protected override IPolygonScorer CreateBehavior(IGH_DataAccess DA)
            => new CloseToCenterPolygonScorer();
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
        }
    }
}
