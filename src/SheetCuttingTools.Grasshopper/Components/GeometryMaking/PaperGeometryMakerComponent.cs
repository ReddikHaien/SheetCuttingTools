﻿using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using GrasshopperAsyncComponent;
using Rhino.Runtime;
using SheetCuttingTools.Abstractions.Contracts;
using SheetCuttingTools.Abstractions.Models;
using SheetCuttingTools.GeometryMaking;
using SheetCuttingTools.GeometryMaking.Models;
using SheetCuttingTools.Grasshopper.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Grasshopper.Components.GeometryMaking;

[Guid("ECC6978D-0F97-44E6-A531-8557B16F33DD")]
public class PaperGeometryMakerComponent() : BaseGeometryMaker("Paper geometry maker", "PGM", "Creates a sheet for paper based cutting")
{
    protected override Bitmap Icon => Icons.GeometryMaker_PaperGeometryMaker;

    protected override ToolWorker CreateWorker()
        => new PaperGeometryMakerWorker(this);

    protected class PaperGeometryMakerWorker(PaperGeometryMakerComponent parent) : ToolWorker(parent)
    {
        IFlattenedGeometry[] segment;
        Sheet[] sheet;
        private double tapLength;
        private double tapSteepness;
        private double labelSize;

        public override void DoWork(Action<string, double> ReportProgress, Action Done)
        {
            if (segment is null)
                return;

            try
            {
                var context = new GeometryMakerContext(segment[0]);
                var maker = new PaperGeometryMaker(tapLength,tapSteepness,labelSize);
                var l = segment.Length;
                var i = 0;
                List<Sheet> sheets = new(l);
                foreach(var s in segment)
                {
                    sheets.Add(maker.CreateSheet(s, context));
                    ReportProgress(Id, (1.0 / l) * i++);
                }

                sheet = [.. sheets];
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

            GH_Number tapLength = new();
            GH_Number tapSteepness = new();
            GH_Number labelSize = new();

            if (!DA.GetData(1, ref tapLength))
                tapLength.Value = 3.0;

            if (!DA.GetData(2, ref tapSteepness))
                tapSteepness.Value = 0.25;

            if (!DA.GetData(3, ref labelSize))
                labelSize.Value = 3;

            this.tapLength = tapLength.Value;
            this.tapSteepness = tapSteepness.Value;
            this.labelSize = labelSize.Value;
        }

        public override void RegisterInputsParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Flattened Segment", "F", "The flattened segment to process", GH_ParamAccess.list);
            pManager.AddNumberParameter("Tap Length", "TL", "How long the taps should be extruded. Default is 3", GH_ParamAccess.item, 3.0);
            pManager.AddNumberParameter("Tap Steepness", "TS", "How much to narrow the top part of taps. Between 0 and 0.5. Default is 0.25", GH_ParamAccess.item, 0.25);
            pManager.AddNumberParameter("Label Size", "LS", "The size of labels. Default is 3.", GH_ParamAccess.item, 3.0);
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
