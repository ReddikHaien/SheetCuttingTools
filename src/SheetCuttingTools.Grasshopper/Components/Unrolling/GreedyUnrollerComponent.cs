using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using GrasshopperAsyncComponent;
using SheetCuttingTools.Abstractions.Behaviors;
using SheetCuttingTools.Abstractions.Contracts;
using SheetCuttingTools.Abstractions.Models;
using SheetCuttingTools.Flattening;
using SheetCuttingTools.Grasshopper.Helpers;
using SheetCuttingTools.Grasshopper.Models;
using SheetCuttingTools.Infrastructure.Progress;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Grasshopper.Components.Unrolling
{
    [Guid("5D6297AC-91E6-4B02-9D19-C467056E9CCE")]
    public class GreedyUnrollerComponent() : BaseUnroller("Greedy unroller", "GU", "Unrolling algorithm that greedily builds segments")
    {
        protected override Bitmap Icon => Icons.Flattening_GreedyFlattener;

        protected override ToolWorker CreateWorker()
            => new GreedyUnrollerWorker(this);

        protected class GreedyUnrollerWorker(GreedyUnrollerComponent parent) : ToolWorker(parent)
        {
            private IGeometry segment;
            private IEdgeFilter[] edgeFilters;
            private IFlattenedSegmentConstraint[] flattenedSegmentConstraints;
            private IPolygonScorer[] polygonScorers;

            private IFlattenedGeometry[] flattened;

            public override void DoWork(Action<string, double> ReportProgress, Action Done)
            {
                if (segment is null)
                    return;
                try
                {
                    var progress = new ToolProgress(Id, ReportProgress);
                    var unroller = new GreedyPolygonSegmentUnroller(flattenedSegmentConstraints, polygonScorers, edgeFilters, progress);
                    flattened = unroller.FlattenSegment(segment, CancellationToken);
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
                if(!DA.GetData(0, ref segment))
                {
                    AddErrorMessage("Missing segment value!");
                    return;
                }

                this.segment = segment.CreateGeometry();


                List<GH_ObjectWrapper> behaviors = [];
                if (!DA.GetDataList(1, behaviors))
                {
                    AddErrorMessage("Missing behaviors!");
                    return;
                }

                List<IFlattenedSegmentConstraint> flattenedSegmentConstraints = [];
                List<IEdgeFilter> edgeFilters = [];
                List<IPolygonScorer> polygonScorers = [];

                foreach (var wrapper in behaviors)
                {
                    if (wrapper.Value is not IBehavior behavior)
                    {
                        AddWarningMessage($"non behavior value provided {wrapper.GetType().Name}");
                        continue;
                    }

                    switch (behavior)
                    {
                        case IFlattenedSegmentConstraint sc:
                            flattenedSegmentConstraints.Add(sc);
                            break;
                        case IEdgeFilter sc:
                            edgeFilters.Add(sc);
                            break;

                        case IPolygonScorer sc:
                            polygonScorers.Add(sc);
                            break;

                        default:
                            AddWarningMessage($"Unsupported behavior type: {behavior.Name()}");
                            break;
                    }
                }

                this.flattenedSegmentConstraints = [.. flattenedSegmentConstraints];
                this.edgeFilters = [.. edgeFilters];
                this.polygonScorers = [.. polygonScorers];
            }

            public override void SetData(IGH_DataAccess DA)
            {
                DA.SetDataList(0, flattened.Select(x => new GH_Geometry(x)));
            }

            public override void RegisterInputsParams(GH_InputParamManager pManager)
            {
                pManager.AddGenericParameter("Segments", "S", "The segments to unroll", GH_ParamAccess.item);
                pManager.AddGenericParameter("Behaviors", "B", "The behaviors to use", GH_ParamAccess.list);
            }

            public override void RegisterOutputParams(GH_OutputParamManager pManager)
            {
                pManager.AddGenericParameter("Sheets", "S", "The unrolled segments", GH_ParamAccess.item);
            }   
        }
    }
}
