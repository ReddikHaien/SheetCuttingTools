using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using SheetCuttingTools.Abstractions.Models;
using SheetCuttingTools.Grasshopper.Helpers;
using SheetCuttingTools.Infrastructure.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Grasshopper.Models
{
    public class GH_Sheet : GH_Goo<Sheet>, IGH_PreviewData
    {
        private readonly Lazy<(string, Point3d[][])[]> curves;
        private readonly Lazy<BoundingBox> box;

        private GH_Sheet(Sheet sheet, Func<(string, Point3d[][])[]> maker) : base(sheet)
        {
            curves = new(maker, System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);
            box = new(() =>
            {
                var t = curves.Value.SelectMany(x => x.Item2).SelectMany(x => x);
                return new BoundingBox(t);

            }, System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);
        }

        public GH_Sheet(Sheet sheet) : this(sheet, () => CreateCurves(sheet))
        {

        }

        public BoundingBox ClippingBox => box.Value;

        public override bool IsValid => true;

        public override string TypeName => "Sheet";

        public override string TypeDescription => "Sheet, ready to be cut";

        public void DrawViewportMeshes(GH_PreviewMeshArgs args)
        {
        }

        public void DrawViewportWires(GH_PreviewWireArgs args)
        {
            foreach(var (name, lines) in curves.Value)
            {
                var color = ColorHelper.GetColor(name.GetHashCode());

                foreach(var line in lines)
                {
                    args.Pipeline.DrawPolyline(line, color);
                }
            }

            var textColor = ColorHelper.GetColor("TextColor".GetHashCode());

            foreach(var (edge, name) in Value.BoundaryNames)
            {
                var (a, b) = Value.FlattenedSegment.GetPoints(edge);
                var p = (a + b) / 2;

                var plane = Plane.WorldXY;

                plane.Origin = p.ToRhinoPoint3d();

                args.Pipeline.Draw3dText(name, textColor, plane, 1.0, "Calibri");
            }
        }

        public override IGH_Goo Duplicate()
            => new GH_Sheet(Value, () => curves.Value);

        public override string ToString()
            => "Sheet";


        private static (string, Point3d[][])[] CreateCurves(Sheet sheet)
        {
            Dictionary<string, Point3d[][]> curves = [];

            return sheet.Lines.Select(
                x => (x.Key, x.Select(x => x.Select(y => y.ToRhinoPoint3d()).ToArray()).ToArray())
            ).ToArray();

        }
    }
}
