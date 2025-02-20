using g3;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using GrasshopperAsyncComponent;
using Rhino.Geometry;
using SheetCuttingTools.Abstractions.Contracts;
using SheetCuttingTools.Grasshopper.Helpers;
using SheetCuttingTools.Grasshopper.Models;
using SheetCuttingTools.Segmentation.Segmentors;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Grasshopper.Components.Segmentation
{
    [Guid("ACE14DBC-D41C-4A77-A02D-CDBEFA7C81AF")]
    public class LayerSlicerSegmentorComponent() : BaseSegmentator("Layer slicer segmentor", "LSS", "Slices a geometry according to a plane")
    {
        protected override Bitmap Icon => Icons.Segmentation_LayerSlicerSegmentor;

        protected override ToolWorker CreateWorker()
            => new LayerSlicerSegmentorWorker(this);

        protected class LayerSlicerSegmentorWorker(LayerSlicerSegmentorComponent parent) : ToolWorker(parent)
        {
            private IGeometry geometry;
            private Plane plane;
            private int numSegments;

            private IGeometry[] producedSegments;

            public override void DoWork(Action<string, double> ReportProgress, Action Done)
            {
                if (geometry is null)
                {
                    AddErrorMessage("Missing geometry");
                    return;
                }

                if (numSegments <= 0)
                {
                    AddErrorMessage("numSegments must be greater than 0");
                    return;
                }

                try
                {
                    var cutPlane = new Plane3d(plane.Normal.ToG3Vector3d(), plane.Origin.ToG3Vector3d());
                    var segmentor = new LayerSlicerSegmentor();
                    producedSegments = segmentor.SegmentGeometry(geometry, cutPlane, numSegments, CancellationToken).Result;
                    Done();
                }
                catch (Exception e) when (e is TaskCanceledException or OperationCanceledException)
                {

                }
                catch (Exception e)
                {
                    AddErrorMessage($"Something went wrong {e}");
                }
            }

            public override WorkerInstance Duplicate()
                => new LayerSlicerSegmentorWorker(parent);

            public override void GetData(IGH_DataAccess DA, GH_ComponentParamServer Params)
            {
                GH_ObjectWrapper geometry = new();
                GH_Plane plane = new();
                GH_Integer numSegments = new();

                if (!DA.GetData(0, ref geometry) || !DA.GetData(1, ref plane) || !DA.GetData(2, ref numSegments))
                    return;

                this.geometry = geometry.CreateGeometry();
                this.plane = plane.Value;
                this.numSegments = numSegments.Value;
            }

            public override void RegisterInputsParams(GH_InputParamManager pManager)
            {
                pManager.AddGenericParameter("Geometry", "G", "The geometry to process", GH_ParamAccess.item);
                pManager.AddPlaneParameter("Cutting plane", "P", "The plane to cut the geometry along", GH_ParamAccess.item);
                pManager.AddIntegerParameter("Segments", "S", "The number of segments to produce", GH_ParamAccess.item);
            }

            public override void RegisterOutputParams(GH_OutputParamManager pManager)
            {
                pManager.AddGenericParameter("Segments", "S", "The produced segments", GH_ParamAccess.list);
            }

            public override void SetData(IGH_DataAccess DA)
            {
                DA.SetDataList(0, producedSegments.Select(g => new GH_Geometry(g)));
            }
        }
    }
}
