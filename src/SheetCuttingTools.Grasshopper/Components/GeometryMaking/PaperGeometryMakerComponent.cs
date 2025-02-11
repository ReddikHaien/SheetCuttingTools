using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using GrasshopperAsyncComponent;
using SheetCuttingTools.Abstractions.Models;
using SheetCuttingTools.GeometryMaking;
using SheetCuttingTools.Grasshopper.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Grasshopper.Components.GeometryMaking;

[Guid("ECC6978D-0F97-44E6-A531-8557B16F33DD")]
public class PaperGeometryMakerComponent() : BaseGeometryMaker("Paper Geometry Maker", "PGM", "Creates a sheet for paper based cutting")
{
    protected override ToolWorker CreateWorker()
        => new PaperGeometryMakerWorker(this);


    protected class PaperGeometryMakerWorker(PaperGeometryMakerComponent parent) : ToolWorker(parent)
    {
        FlattenedSegment segment;
        Sheet sheet;

        public override void DoWork(Action<string, double> ReportProgress, Action Done)
        {
            if (segment is null)
                return;

            try
            {
                var maker = new PaperGeometryMaker();
                sheet = maker.CreateSheet(segment);
                Done();
            }
            catch(Exception e)
            {
                AddErrorMessage($"Something went wrong: {e}");
            }
        }

        public override WorkerInstance Duplicate()
            => parent.CreateWorker();

        public override void GetData(IGH_DataAccess DA, GH_ComponentParamServer Params)
        {
            GH_ObjectWrapper segment = new();
            if (!DA.GetData(0, ref segment))
            {
                AddErrorMessage("Failed to get segment");
                return;
            }

            if (segment.Value is GH_FlattenedSegment ghflat)
            {
                this.segment = ghflat.Value;
                return;
            }
            if (segment.Value is FlattenedSegment flat)
            {
                this.segment = flat;
                return;
            }
            
            AddErrorMessage("Provided value is not a flat segment!");
            
        }

        public override void RegisterInputsParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Flattened Segment", "F", "The flattened segment to process", GH_ParamAccess.item);
        }

        public override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Sheet", "S", "The sheet that can be laser cut", GH_ParamAccess.item);
        }

        public override void SetData(IGH_DataAccess DA)
        {
            DA.SetData(0, new GH_Sheet(sheet));
        }
    }
}
