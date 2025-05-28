using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using SheetCuttingTools.Abstractions.Behaviors;
using SheetCuttingTools.GeometryMaking.Parts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Grasshopper.Components.Behaviors.PartMakers
{
    [Guid("BB04B2F6-6BAB-49A6-97A9-9A4A4F331FFE")]
    public class JaggedConnectorPartMakerComponent() : BasePartMaker("Jagged Connector Part Maker", "JCPM", "Part maker for jagged connectors")
    {
        protected override IPartMaker CreateBehavior(IGH_DataAccess DA)
        {
            GH_Integer numTheeth = new();
            GH_Number gapSize = new();
            GH_Number theethHeight = new();

            if (!DA.GetData(0, ref numTheeth))
                numTheeth.Value = 3;
            
            if (!DA.GetData(1, ref gapSize))
                gapSize.Value = 3;
            
            if (!DA.GetData(0, ref theethHeight))
                theethHeight.Value = 4;

            return new JaggedConnectorPartMaker(numTheeth.Value, gapSize.Value, theethHeight.Value);
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("Num theeth", "N", "The number of theeth to make", GH_ParamAccess.item, 3);
            pManager.AddNumberParameter("Gap size", "G", "The gap size between polygons", GH_ParamAccess.item, 3);
            pManager.AddNumberParameter("Theeth Height", "H", "The height of each theeth, should be greaater than gap size", GH_ParamAccess.item, 4);
        }
    }
}
