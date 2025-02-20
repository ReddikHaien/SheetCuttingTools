using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using GrasshopperAsyncComponent;
using Rhino.Geometry;
using SheetCuttingTools.Abstractions.Contracts;
using SheetCuttingTools.Abstractions.Models;
using SheetCuttingTools.Grasshopper.Helpers;
using SheetCuttingTools.Grasshopper.Models;
using SheetCuttingTools.Grasshopper.Models.Internal;
using SheetCuttingTools.Segmentation.Segmentors;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace SheetCuttingTools.Grasshopper.Components.Segmentation
{
    [Guid("9E6E4E4A-724C-45C3-B5C3-46966AECEA7C")]
    public class CakeSlicerSegmentatorComponent() : BaseSegmentator("Cake slicer segmentator", "CSS", "Segments a geometry using radial slicing")
    {
        protected override Bitmap Icon => Icons.Segmentation_CakeSlicerSegmentor;

        protected override ToolWorker CreateWorker()
            => new CakeSlicerSegmentatorWorker(this);

        protected class CakeSlicerSegmentatorWorker(CakeSlicerSegmentatorComponent parent) : ToolWorker(parent)
        {

            private IGeometry geometry;
            private int numSegments;
            private Plane cutPlane;
            private IGeometry[] result;
            public override void DoWork(Action<string, double> ReportProgress, Action Done)
            {
                if (geometry is null)
                    return;

                try
                {
                    var segmentor = new CakeSlicerSegmentor();

                    var origin = cutPlane.Origin.ToG3Vector3d();
                    var zAxis = cutPlane.ZAxis.ToG3Vector3d();
                    var xAxis = cutPlane.XAxis.ToG3Vector3d();

                    result = segmentor.SegmentGeometry(geometry, numSegments, origin, zAxis, xAxis, CancellationToken).Result;
                    Done();
                }
                catch (Exception e) when (e is not (TaskCanceledException or OperationCanceledException))
                {
                    AddErrorMessage($"Something went wrong: {e}");
                }

            }

            public override WorkerInstance Duplicate()
                => new CakeSlicerSegmentatorWorker(parent);

            public override void GetData(IGH_DataAccess DA, GH_ComponentParamServer Params)
            {
                GH_ObjectWrapper surface = new();
                if (!DA.GetData(0, ref surface))
                {
                    AddErrorMessage("Missing geometry value");
                    return;
                }

                GH_Integer segments = new();
                if (!DA.GetData(1, ref segments))
                {
                    AddErrorMessage("Missing number of segments");
                    return;
                }

                GH_Plane direction = new();
                if (!DA.GetData(2, ref direction))
                {
                    AddErrorMessage("Missing cut plane");
                    return;
                }
                geometry = surface.CreateGeometry();
                numSegments = segments.Value;
                cutPlane = direction.Value;
            }

            public override void RegisterInputsParams(GH_InputParamManager pManager)
            {
                pManager.AddGenericParameter("Geometry", "G", "The input geometry", GH_ParamAccess.item);
                pManager.AddIntegerParameter("Segments", "S", "The number of segments to make", GH_ParamAccess.item, 5);
                pManager.AddPlaneParameter("Cut Plane", "C", "The plane to cut on, the model will be cut along the normal", GH_ParamAccess.item, Plane.WorldXY);
            }

            public override void RegisterOutputParams(GH_OutputParamManager pManager)
            {
                pManager.AddGenericParameter("Segments", "S", "The produced segments", GH_ParamAccess.list);
            }

            public override void SetData(IGH_DataAccess DA)
            {
                DA.SetDataList(0, result.Select(x => new GH_Geometry(x)));
            }
        }
    }
}
