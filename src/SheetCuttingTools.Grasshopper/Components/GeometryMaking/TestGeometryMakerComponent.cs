using Grasshopper.Kernel.Types;
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

namespace SheetCuttingTools.Grasshopper.Components.GeometryMaking
{

    [Guid("409D08F4-FECC-4A61-B91B-A4134F267808")]
    public class TestGeometryMakerComponent() : BaseGeometryMaker("Test geometry maker", "TGM", "Component that only generates the boundary and fold edges")
    {
        protected override ToolWorker CreateWorker()
            => new TestGeometryMakerWorker(this);

        protected class TestGeometryMakerWorker(TestGeometryMakerComponent parent) : ToolWorker(parent)
        {
            IFlattenedGeometry[] segment;
            Sheet[] sheet;

            public override void DoWork(Action<string, double> ReportProgress, Action Done)
            {
                if (segment is null)
                    return;

                try
                {
                    var maker = new TestGeometryMaker();
                    var context = new GeometryMakerContext();
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
            }

            public override void RegisterInputsParams(GH_InputParamManager pManager)
            {
                pManager.AddGenericParameter("Flattened Geometry", "F", "The flattened segment to process", GH_ParamAccess.list);
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
