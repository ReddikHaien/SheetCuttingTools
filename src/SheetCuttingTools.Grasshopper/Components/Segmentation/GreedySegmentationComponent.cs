using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using GrasshopperAsyncComponent;
using Rhino.Geometry;
using SheetCuttingTools.Abstractions.Behaviors;
using SheetCuttingTools.Abstractions.Contracts;
using SheetCuttingTools.Abstractions.Models;
using SheetCuttingTools.Grasshopper.Helpers;
using SheetCuttingTools.Grasshopper.Models;
using SheetCuttingTools.Infrastructure.Progress;
using SheetCuttingTools.Segmentation;
using SheetCuttingTools.Segmentation.Segmentors;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

namespace SheetCuttingTools.Grasshopper.Components.Segmentation
{
    [Guid("1DD5C063-E735-4C2F-9A72-70F9355155B0")]
    public class GreedySegmentationComponent() : BaseSegmentator(
            "Greedy Segmentator",
            "GS",
            "A greedy segmentation algorithm that builds segments 'first-come-first-serve' style")
    {
        protected override Bitmap Icon => Icons.Segmentation_GreedySegmentor;

        protected override ToolWorker CreateWorker()
            => new GreedySegmentationWorker(this);

        class GreedySegmentationWorker(GreedySegmentationComponent parent) : ToolWorker(parent)
        {
            private static readonly string GeometryDescription = $"The input geometry, This value is expected to be a surface, mesh, '{typeof(Model).Name}', '{typeof(Segment).Name}' ";
            private static readonly string BehaviorDescription = $"The behaviors, Supported values are: \n{typeof(ISegmentConstraint).Name}\n{typeof(IEdgeFilter).Name}\n{typeof(IPolygonScorer).Name}";

            private IGeometry geometry;
            private ISegmentConstraint[] segmentConstraints;
            private IEdgeFilter[] edgeFilters;
            private IPolygonScorer[] polygonScorers;

            private IGeometry[] segments;

            public override void DoWork(Action<string, double> ReportProgress, Action Done)
            {
                if (geometry is null || segmentConstraints is null || edgeFilters is null || polygonScorers is null)
                    return;
                try
                {
                    var progress = new ToolProgress(Id, ReportProgress);
                    var segmentor = new GreedySegmentor(polygonScorers, edgeFilters, segmentConstraints, progress);
                    segments = segmentor.SegmentateModel(geometry, CancellationToken);
                    Done();
                }catch(Exception e)
                {
                    AddErrorMessage($"Something went wrong: {e}");
                }
            }

            public override WorkerInstance Duplicate()
                => new GreedySegmentationWorker((GreedySegmentationComponent)Parent);

            public override void GetData(IGH_DataAccess DA, GH_ComponentParamServer Params)
            {

                GH_ObjectWrapper geometry = new();

                if (!DA.GetData(0, ref geometry))
                {
                    AddErrorMessage("Missing geometry value!");
                    return;
                }

                this.geometry = geometry.Value.CreateGeometry();

                List<GH_ObjectWrapper> behaviors = [];

                if (!DA.GetDataList(1, behaviors))
                {
                    AddWarningMessage("No behaviors provided");
                    return;
                }

                List<ISegmentConstraint> segmentConstraints = [];
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
                        case ISegmentConstraint sc: 
                            segmentConstraints.Add(sc);
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

                this.segmentConstraints = [.. segmentConstraints];
                this.edgeFilters = [.. edgeFilters];
                this.polygonScorers = [.. polygonScorers];
            }

            public override void RegisterInputsParams(GH_InputParamManager pManager)
            {
                pManager.AddGenericParameter("Geometry", "G", GeometryDescription, GH_ParamAccess.item);
                pManager.AddGenericParameter("Behaviors", "B", BehaviorDescription, GH_ParamAccess.list);
            }

            public override void RegisterOutputParams(GH_OutputParamManager pManager)
            {
                pManager.AddGenericParameter("Segments", "S", "The produced segments from the input geometry", GH_ParamAccess.item);
            }

            public override void SetData(IGH_DataAccess DA)
            {
                DA.SetDataList(0, segments.Select(x => new GH_Geometry(x)));
            }
        }
    }
}
