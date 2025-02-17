using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using GrasshopperAsyncComponent;
using SheetCuttingTools.Abstractions.Contracts;
using SheetCuttingTools.Grasshopper.Helpers;
using SheetCuttingTools.Grasshopper.Models;
using SheetCuttingTools.Segmentation.Segmentors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Grasshopper.Components.Segmentation
{
    [Guid("E4424777-1C78-496D-B1FA-067B84E94272")]
    public class CakeSlicerV2Component() : BaseSegmentator("Cake slicer V2 segmentor", "CSV2", "V2 Cake slicer, using different implementation")
    {
        protected override ToolWorker CreateWorker()
            => new CakeSlicerV2Worker(this);

        protected class CakeSlicerV2Worker(CakeSlicerV2Component parent) : ToolWorker(parent)
        {
            IGeometry geometry;
            IGeometry[] result;
            public override void DoWork(Action<string, double> ReportProgress, Action Done)
            {
                if (geometry is null)
                    return;

                try
                {
                    var segmentor = new CakeSlicerSegmentor();
                    //var segments = segmentor.SegmentGeometry(geometry, 4, g3.Vector3d.Zero, g3.Vector3d.AxisZ, g3.Vector3d.AxisX, CancellationToken).Result;
                    result = [geometry];
                    Done();
                }
                catch(Exception e) when(e is not (TaskCanceledException or OperationCanceledException))
                {
                    AddErrorMessage($"Something went wrong: {e}");
                }

            }

            public override WorkerInstance Duplicate()
                => new CakeSlicerV2Worker(parent);

            public override void GetData(IGH_DataAccess DA, GH_ComponentParamServer Params)
            {
                GH_ObjectWrapper g = new();
                if (!DA.GetData(0, ref g))
                {
                    AddErrorMessage("Failed to get geometry");
                    return;
                }

                geometry = g.CreateGeometry();

            }

            public override void RegisterInputsParams(GH_InputParamManager pManager)
            {
                pManager.AddGenericParameter("Geometry", "G", "The geometry data to use", GH_ParamAccess.item);
            }

            public override void RegisterOutputParams(GH_OutputParamManager pManager)
            {
                pManager.AddGenericParameter("Segments", "S", "The produced segments", GH_ParamAccess.list);
            }

            public override void SetData(IGH_DataAccess DA)
            {
                DA.SetDataList(0, result.Select(x => new GH_SegmentV2(x)));
            }
        }
    }
}
