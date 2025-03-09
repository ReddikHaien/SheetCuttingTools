using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using SheetCuttingTools.Abstractions.Behaviors;
using SheetCuttingTools.GeometryMaking.Parts;
using SheetCuttingTools.Grasshopper.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Grasshopper.Components.Behaviors.PartMakers
{
    [Guid("0A427E1B-5D39-4875-BA48-A98811D4CA8C")]
    public class ManualPartMakerComponent() : BasePartMaker("Manual Part Maker", "MPM", "Part maker for manually specifying geometry. The geometry will be transformed to fit the final model.")
    {
        protected override IPartMaker CreateBehavior(IGH_DataAccess DA)
        {
            GH_Number gap = new();

            if (!DA.GetData(0, ref gap))
                gap.Value = 3;

            List<GH_Curve> curves = [];

            if (!DA.GetDataList(1, curves))
                return null;

            var arr = curves.Select(x => x.Value).ToArray();

            var (lines, circles) = ParseCurves(arr);

            return new CustomPartMaker(gap.Value, (ctx, output) => GenerateGeo(ctx, output, lines, circles));
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Gap size", "G", "The gap size between polygons", GH_ParamAccess.item, 3);
            pManager.AddCurveParameter("Lines", "L", "Lines to add to the geometry, currently supports PolyLine, Line and Circle", GH_ParamAccess.list);
        }

        private static void GenerateGeo(IPartMakerContext ctx, IPartGeometryOutput output, g3.Vector2d[][] lines, g3.Circle2d[] circles)
        {
            var u = ctx.U;
            var v = ctx.V;
            var a = ctx.A;
            var b = ctx.B;

            foreach (var line in lines)
            {
                output.AddLine(line.Select(p => a + u*p.x + v*p.y));
            }

            foreach(var circle in circles)
            {
                output.AddCircle(a + circle.Center.x * u + circle.Center.y * v, circle.Radius);
            }
        }

        private static (g3.Vector2d[][] Lines, g3.Circle2d[] circles) ParseCurves(Curve[] curves)
        {
            List<g3.Vector2d[]> lines = [];
            List<g3.Circle2d> circles = [];  
            var bounds = curves.Aggregate(BoundingBox.Empty, (b, c) => BoundingBox.Union(b, c.GetBoundingBox(true)));
            var width = bounds.Diagonal.X;
            var height = bounds.Diagonal.Y;

            foreach(var curve in curves)
            {
                if (curve.TryGetPolyline(out var polyLine))
                {
                    List < g3.Vector2d > line = [];
                    foreach(var p in polyLine)
                    {
                        double x = p.X / width;
                        double y = p.Y / height;
                        line.Add(new(x, y));
                    }

                    continue;
                }
               
                if (curve.TryGetCircle(out var circle))
                {
                    double x = circle.Center.X / width;
                    double y = circle.Center.Y / height;
                    circles.Add(new g3.Circle2d(new(x, y), circle.Radius));
                }
            }

            return (lines.ToArray(), circles.ToArray());
        }
    }
}
