using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using GrasshopperAsyncComponent;
using SheetCuttingTools.Abstractions.Behaviors;
using SheetCuttingTools.Abstractions.Contracts;
using SheetCuttingTools.Flattening;
using SheetCuttingTools.Grasshopper.Helpers;
using SheetCuttingTools.Grasshopper.Models;
using SheetCuttingTools.Infrastructure.Progress;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace SheetCuttingTools.Grasshopper.Components.Unrolling
{
    [Guid("8D4C88D3-3382-40EE-83DF-D0D73F5CD22E")]
    public class StripUnrollerComponent() : BaseUnroller("Strip unroller", "SU", "Unrolls geometry based on strips")
    {
        protected override ToolWorker CreateWorker()
            => new StripUnrollerWorker(this);

        protected class StripUnrollerWorker(StripUnrollerComponent parent) : ToolWorker(parent)
        {
            private IGeometry segment;
            private IFlattenedSegmentConstraint[] flattenedSegmentConstraints;
            private IFlattenedGeometry[] flattened;

            public override void DoWork(Action<string, double> ReportProgress, Action Done)
            {
                if (segment is null)
                    return;
                try
                {
                    var progress = new ToolProgress(Id, ReportProgress);
                    var unroller = new StripSegmentUnroller(flattenedSegmentConstraints);
                    flattened = unroller.UnrollSegment(segment, CancellationToken);
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

                this.segment = segment.CreateGeometry();
                this.flattenedSegmentConstraints = behaviors.Select(x => x.Value as IFlattenedSegmentConstraint).Where(x => x is not null).ToArray();
            }
            

            public override void SetData(IGH_DataAccess DA)
            {
                DA.SetDataList(0, flattened.Select(x => new GH_Geometry(x)));
            }

            public override void RegisterInputsParams(GH_InputParamManager pManager)
            {
                pManager.AddGenericParameter("Segments", "S", "The segments to unroll", GH_ParamAccess.item);
                pManager.AddGenericParameter("Behaviors", "B", "The behaviors used by this component. Supported behaviors are Flattened segment constraints", GH_ParamAccess.list);
            }

            public override void RegisterOutputParams(GH_OutputParamManager pManager)
            {
                pManager.AddGenericParameter("Sheets", "S", "The unrolled segments", GH_ParamAccess.item);
            }
        }
    }
}
