using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using SheetCuttingTools.Abstractions.Models;
using SheetCuttingTools.Grasshopper.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;

namespace SheetCuttingTools.Grasshopper.Models
{
    public class GH_FlattenedSegment : GH_Goo<FlattenedSegment>, IGH_PreviewData
    {
        public GH_FlattenedSegment(FlattenedSegment segment) : this(segment, () => CreateSheetMesh(segment))
        { }

        private GH_FlattenedSegment(FlattenedSegment segment, Func<ReadOnlyCollection<Polyline>> meshMaker) : base(segment)
        {
            this.segment = segment;
            mesh = new Lazy<ReadOnlyCollection<Polyline>>(meshMaker, System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);
        }

        private readonly FlattenedSegment segment;
        private readonly Lazy<ReadOnlyCollection<Polyline>> mesh;

        public ReadOnlyCollection<Polyline> Mesh => mesh.Value;

        public BoundingBox ClippingBox => Mesh.Select(x => x.BoundingBox).Aggregate(BoundingBox.Union);

        public override bool IsValid => Mesh.All(x => x.IsValid);

        public override string TypeName => "FlattenedSegment";

        public override string TypeDescription => "A segment that has been unrolled into a 2D sheet";

        public void DrawViewportMeshes(GH_PreviewMeshArgs args)
        {
        }

        public void DrawViewportWires(GH_PreviewWireArgs args)
        {
            var color = ColorHelper.GetColor(segment.GetHashCode());
            foreach(var polygon in Mesh)
            {
                args.Pipeline.DrawPolyline(polygon, color);
            }
        }

        public override IGH_Goo Duplicate()
            => new GH_FlattenedSegment(segment, () => Mesh);

        public override string ToString()
            => $"Flattened Segment, polys: {segment.Polygons.Length}, points: {segment.Points.Length}";

        private static ReadOnlyCollection<Polyline> CreateSheetMesh(FlattenedSegment segment)
        {
            var curves = new List<Polyline>();

            foreach(var polygon in segment.Polygons)
            {
                var curve = new Polyline(polygon.Placed.Points.Length+1);

                curve.AddRange(polygon.Placed.Points.Select(x => (Point3d)segment.Points[x].ToPoint3f()));
                curve.Add(curve[0]);
                curves.Add(curve);
            }

            return curves.AsReadOnly();
        }
    }
}
