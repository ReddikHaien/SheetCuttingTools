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
    internal class GH_SegmentV2(IGeometry geometry) : GH_Goo<IGeometry>(geometry), IGH_PreviewData
    {
        private static readonly ConditionalWeakTable<IGeometry, Mesh> cachedMeshes = [];

        public override bool IsValid => true;

        public override string TypeName => "Geometry";

        public override string TypeDescription => "A processed Geometry";

        public BoundingBox ClippingBox => GetMesh(Value).GetBoundingBox(false);

        public override IGH_Goo Duplicate()
            => new GH_SegmentV2(Value);

        public override string ToString()
            => $"Geometry: {Value.Polygons.Count} polygons";


        public void DrawViewportWires(GH_PreviewWireArgs args)
        {
            args.Pipeline.DrawMeshWires(GetMesh(Value), Color.Black);
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
