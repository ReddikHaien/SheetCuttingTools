using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using SheetCuttingTools.Abstractions.Contracts;
using SheetCuttingTools.Grasshopper.Helpers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Grasshopper.Models
{
    internal class GH_Geometry(IGeometry geometry) : GH_Goo<IGeometry>(geometry), IGH_PreviewData
    {
        private static readonly ConditionalWeakTable<IGeometry, Mesh> cachedMeshes = [];
        private static readonly ConditionalWeakTable<IFlattenedGeometry, Vector3d[][]> cachedCurves = [];

        public override bool IsValid => true;

        public override string TypeName => Value is IFlattenedGeometry ? "Flattened Geometry" : "Geometry";

        public override string TypeDescription => "A processed Geometry";

        public BoundingBox ClippingBox => GetMesh(Value).GetBoundingBox(false);

        public override IGH_Goo Duplicate()
            => new GH_Geometry(Value);

        public override string ToString()
            => $"Geometry: {Value.Polygons.Count} polygons";


        public void DrawViewportWires(GH_PreviewWireArgs args)
        {
            var color = ColorHelper.GetColor(Value.GetHashCode());

            color = Color.FromArgb(color.R / 2, color.G / 2, color.B / 2);

            args.Pipeline.DrawMeshWires(GetMesh(Value), color);
            if (Value is IFlattenedGeometry flattened)
            {
                //foreach(var (_, placed) in flattened.PlacedPolygons)
                //{
                //    args.Pipeline.DrawPolygon(placed.Points.Select(x => flattened.Points[x].ToRhinoPoint3d()), color, false);
                //}
            }
        }

        public void DrawViewportMeshes(GH_PreviewMeshArgs args)
        {
            args.Pipeline.DrawMeshFalseColors(GetMesh(Value));
        }

        public static Mesh GetMesh(IGeometry geometry)
        {
            if (!cachedMeshes.TryGetValue(geometry, out var mesh))
            {
                mesh = geometry.CreateRhinoMesh();
                cachedMeshes.Add(geometry, mesh);
            }

            return mesh;
        }
    }
}
