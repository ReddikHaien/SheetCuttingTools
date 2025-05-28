using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using GrasshopperAsyncComponent;
using Rhino.Geometry;
using SheetCuttingTools.Abstractions.Behaviors;
using SheetCuttingTools.Abstractions.Contracts;
using SheetCuttingTools.Flattening;
using SheetCuttingTools.Grasshopper.Helpers;
using SheetCuttingTools.Grasshopper.Models;
using SheetCuttingTools.Infrastructure.Progress;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;

namespace SheetCuttingTools.Grasshopper.Components.Unrolling
{
    [Guid("8D4C88D3-3382-40EE-83DF-D0D73F5CD22E")]
    public class StripUnrollerComponent() : BaseUnroller("Strip unroller", "SU", "Unrolls geometry by greedily building up strips")
    {
        protected override Bitmap Icon => Icons.Unrolling_StripUnroller;
        protected override ToolWorker CreateWorker()
            => new StripUnrollerWorker(this);

        protected class StripUnrollerWorker(StripUnrollerComponent parent) : ToolWorker(parent)
        {
            private IGeometry segment;
            private IFlattenedSegmentConstraint[] flattenedSegmentConstraints;
            private Vector3d preferredStripDirection;
            private bool treatDirectionAsPlane;
            private IFlattenedGeometry[] flattened;

            public override void DoWork(Action<string, double> ReportProgress, Action Done)
            {
                if (segment is null)
                    return;

                try
                {
                    var progress = new ToolProgress(Id, ReportProgress);
                    var unroller = new StripSegmentUnroller2(flattenedSegmentConstraints, treatDirectionAsPlane);// preferredStripDirection.ToG3Vector3d(), treatDirectionAsPlane);
                    flattened = unroller.UnrollSegment(segment, progress, CancellationToken);
                    if (!CancellationToken.IsCancellationRequested)
                        Done();
                }
                catch (Exception e)
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
                    AddErrorMessage("Missing segment value!");
                    return;
                }

                List<GH_ObjectWrapper> behaviors = [];

                if (!DA.GetDataList(1, behaviors))
                {
                    return;
                }

                if (!behaviors.All(x => x.Value is IFlattenedSegmentConstraint))
                {
                    AddWarningMessage("Some behaviors are not supported");
                }

                GH_Vector preferredDirection = new();
                if (!DA.GetData(2, ref preferredDirection))
                {
                    preferredDirection.Value = Vector3d.ZAxis;
                }

                GH_Boolean treatDirectionAsPlane = new();
                if (!DA.GetData(3, ref treatDirectionAsPlane))
                {
                    treatDirectionAsPlane.Value = false;
                }

                this.segment = segment.CreateGeometry();
                this.treatDirectionAsPlane = treatDirectionAsPlane.Value;
                flattenedSegmentConstraints = behaviors.Select(x => x.Value as IFlattenedSegmentConstraint).Where(x => x is not null).ToArray();
                preferredStripDirection = preferredDirection.Value;
                preferredStripDirection.Unitize();
            }
            

            public override void SetData(IGH_DataAccess DA)
            {
                DA.SetDataList(0, flattened.Select(x => new GH_Geometry(x)));
            }

            public override void RegisterInputsParams(GH_InputParamManager pManager)
            {
                pManager.AddGenericParameter("Segments", "S", "The segments to unroll", GH_ParamAccess.item);
                pManager.AddGenericParameter("Behaviors", "B", "The behaviors used by this component. Supported behaviors are Flattened segment constraints", GH_ParamAccess.list);
                pManager.AddVectorParameter("Preferred direction", "P", "The preferred direction of the strips, default is along global Z", GH_ParamAccess.item, Vector3d.ZAxis);
                pManager.AddBooleanParameter("Treat direction as plane", "T", "Wether the direction should be treated as the normal of the planes the strips should follow", GH_ParamAccess.item, false);
            }

            public override void RegisterOutputParams(GH_OutputParamManager pManager)
            {
                pManager.AddGenericParameter("Sheets", "S", "The unrolled segments", GH_ParamAccess.item);
            }
        }
    }
}
