﻿using Grasshopper.Kernel.Types;
using Grasshopper.Kernel;
using GrasshopperAsyncComponent;
using SheetCuttingTools.Abstractions.Contracts;
using SheetCuttingTools.Abstractions.Models;
using SheetCuttingTools.GeometryMaking.Models;
using SheetCuttingTools.GeometryMaking;
using SheetCuttingTools.Grasshopper.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using SheetCuttingTools.GeometryMaking.Parts;
using SheetCuttingTools.Abstractions.Behaviors;

namespace SheetCuttingTools.Grasshopper.Components.GeometryMaking
{

    [Guid("409D08F4-FECC-4A61-B91B-A4134F267808")]
    public class PartGeometryMakerComponent() : BaseGeometryMaker("Part geometry maker", "PTGM", "Component that generates geometry based on part makers.")
    {
        protected override ToolWorker CreateWorker()
            => new TestGeometryMakerWorker(this);

        protected class TestGeometryMakerWorker(PartGeometryMakerComponent parent) : ToolWorker(parent)
        {
            IFlattenedGeometry[] segment;

            IPartMaker partMaker;
            IEdgeFilter[] edgeFilters;
            bool addlabels;

            Sheet[] sheet;

            public override void DoWork(Action<string, double> ReportProgress, Action Done)
            {
                if (segment is null || partMaker is null)
                    return;

                try
                {
                    var maker = new PartGeometryMaker2(partMaker, edgeFilters, addlabels);
                        var context = new GeometryMakerContext(segment[0]);

                    var l = segment.Length;
                    var i = 0;
                    List<Sheet> sheets = new(l);
                    foreach (var s in segment)
                    {
                        sheets.Add(maker.CreateSheet(s, context));
                        ReportProgress(Id, (1.0 / l) * i++);
                    }

                    sheet = [.. sheets];
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
                List<GH_ObjectWrapper> segment = [];
                if (!DA.GetDataList(0, segment))
                {
                    AddErrorMessage("Failed to get segments");
                    return;
                }

                if (!segment.All(x => x is not null && x.Value is GH_Geometry or IFlattenedGeometry))
                {
                    AddErrorMessage("Provided value is not all flat segments");
                    return;
                }

                this.segment = segment.Select(x => x.Value switch
                {
                    GH_Geometry f => f.Value as IFlattenedGeometry,
                    IFlattenedGeometry f => f,
                    _ => throw new UnreachableException("Should be handeled above")
                }).ToArray();

                if (segment.Any(x => x is null))
                {
                    throw new InvalidOperationException("Non flattened geometry provided");
                }

                GH_ObjectWrapper partMaker = new();
                if (!DA.GetData(1, ref partMaker))
                {
                    AddErrorMessage("Missing hinge maker");
                    return;
                }

                List<GH_ObjectWrapper> edgeFilters = [];
                DA.GetDataList(2, edgeFilters);

                this.edgeFilters = edgeFilters
                    .Select(x => x.Value as IEdgeFilter)
                    .Where(x => x is not null)
                    .ToArray();

                GH_Boolean addLabels = new();
                if (!DA.GetData(3, ref addLabels))
                    addLabels.Value = false;

                this.partMaker = partMaker.Value as IPartMaker;
                if(this.partMaker is null)
                {
                    AddErrorMessage("Invalid part makers provided");
                }
            }

            public override void RegisterInputsParams(GH_InputParamManager pManager)
            {
                pManager.AddGenericParameter("Flattened Geometry", "F", "The flattened segment to process", GH_ParamAccess.list);
                pManager.AddGenericParameter("Part maker", "P", "Part maker to use", GH_ParamAccess.item);
                pManager.AddGenericParameter("Edge filters", "E", "Edge filters to use for selecting the correct edges to process.", GH_ParamAccess.list);
                pManager.AddBooleanParameter("Add labels", "L", "If this part maker should mark all edges with labels", GH_ParamAccess.item, false);
            }

            public override void RegisterOutputParams(GH_OutputParamManager pManager)
            {
                pManager.AddGenericParameter("Sheet", "S", "The sheet that can be laser cut", GH_ParamAccess.item);
            }

            public override void SetData(IGH_DataAccess DA)
            {
                DA.SetDataList(0, sheet.Select(x => new GH_Sheet(x)));
            }
        }
    }
}
