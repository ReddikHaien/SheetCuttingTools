using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using GrasshopperAsyncComponent;
using Rhino.Geometry;
using SheetCuttingTools.Abstractions.Models;
using SheetCuttingTools.Grasshopper.Helpers;
using SheetCuttingTools.Grasshopper.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace SheetCuttingTools.Grasshopper.Components.Segmentation
{
    [Guid("9E6E4E4A-724C-45C3-B5C3-46966AECEA7C")]
    public class CakeSlicerSegmentatorComponent() : BaseSegmentator("CakeSlicerSegmentator", "CSS", "Segments a geometry using radial slicing")
    {
        protected override ToolWorker CreateWorker()
            => new CakeSlicerSegmentatorWorker(this);

        protected class CakeSlicerSegmentatorWorker(CakeSlicerSegmentatorComponent parent) : ToolWorker(parent)
        {
            private Brep brep;
            private Plane cutPlane;
            private int numSegments;
            private Segment[] segments;
            private Brep[] planes;

            public override void DoWork(Action<string, double> ReportProgress, Action Done)
            {
                try
                {

                    if (brep is null)
                        return;

                    var radPerSplit = Math.PI * 2 / numSegments;

                    var bound = brep.GetBoundingBox(true);

                    var radius = (bound.Max - bound.Center).Length;

                    List<Brep> curSplit = [brep];

                    List<Brep> all = [];

                    var planes = new List<Brep>();


                    var zaxis = cutPlane.ZAxis;
                    var yaxis = cutPlane.YAxis;
                    var xaxis = cutPlane.XAxis;

                    var plane = new Plane(Point3d.Origin, xaxis, yaxis);

                    for (int i = 0; i < numSegments; i++)
                    {
                        CancellationToken.ThrowIfCancellationRequested();
                        (double sin, double cos) = Math.SinCos(i * radPerSplit);

                        var p1 = plane.PointAt(sin * radius, cos * radius) + bound.Center - plane.Normal * radius;
                        (sin, cos) = Math.SinCos(i * radPerSplit + radPerSplit);
                        var p2 = plane.PointAt(sin * radius, cos * radius) + bound.Center - plane.Normal * radius;
                        (sin, cos) = Math.SinCos(i * radPerSplit + radPerSplit / 2);
                        var p3 = plane.PointAt(sin * radius, cos * radius) + bound.Center - plane.Normal * radius;


                        var curve = new Polyline(5);
                        curve.AddRange([p1, bound.Center - plane.Normal * radius, p2, p3, p1]);

                        var cutter = Surface.CreateExtrusion(curve.ToPolylineCurve(), plane.Normal * radius * 2);

                        var cb = Brep.CreateFromSurface(cutter);
                        cb.Flip();
                        planes.Add(cb);

                        foreach (var split in curSplit)
                        {
                            var brep = split.Trim(cb, 0.01);

                            brep = Brep.CreateBooleanUnion(brep, 0.01) ?? brep;
                            if (brep is not null)
                                all.AddRange(brep);
                            //all.AddRange(x);
                        }
                    }

                    //for (int i = 0; i < splits; i++)
                    //{
                    //    CancellationToken.ThrowIfCancellationRequested();
                    //    var plane = Plane.CreateFromNormal(Point3d.Origin, Vector3d.YAxis);

                    //    var splitter = new PlaneSurface(plane).ToBrep();
                    //    splitter.Scale(width);
                    //    splitter.Translate((bound.Center - Point3d.Origin) + (Vector3d.XAxis + Vector3d.ZAxis) * -width/2);
                    //    splitter.Rotate(radPerSplit * i, Vector3d.ZAxis, bound.Center);
                    //    planes[i] = splitter;

                    //    List<Brep> next = [];


                    //    var s1 = Plane.CreateFromNormal(bound.Center, Vector3d.YAxis);
                    //    var s2 = Plane.CreateFromNormal(bound.Center, -Vector3d.YAxis);

                    //    s1.Rotate(radPerSplit * i, Vector3d.ZAxis);
                    //    s2.Rotate(radPerSplit * i, Vector3d.ZAxis);

                    //    foreach(var split in curSplit)
                    //    {
                    //        split.

                    //        var x = split.Trim(s1, 0.01);
                    //        var y = split.Trim(s2, 0.01);
                    //        next.AddRange([..x, ..y]);
                    //    }

                    //    all.AddRange(next);

                    //    curSplit = next;
                    //}

                    List<Segment> segments = [];

                    for (int i = 0; i < all.Count; i++)
                    {
                        CancellationToken.ThrowIfCancellationRequested();

                        var geo = Mesh.CreateFromBrep(all[i], MeshingParameters.QualityRenderMesh).Select(x => x.CreateGeometryProvider()).ToArray();

                        segments.AddRange(geo.Select(x => new Segment(x)
                        {
                            Id = Guid.NewGuid(),
                            Polygons = [.. x.Polygons]
                        }));

                        ReportProgress(Id, (1.0 / numSegments) * i);
                    }

                    this.segments = [.. segments];
                    this.planes = planes.ToArray();
                    Done();
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    AddErrorMessage($"Something went wrong: {e}");
                }
            }

            public override WorkerInstance Duplicate()
                => new CakeSlicerSegmentatorWorker(parent);

            public override void GetData(IGH_DataAccess DA, GH_ComponentParamServer Params)
            {
                GH_Brep surface = new();
                if (!DA.GetData(0, ref surface))
                {
                    AddErrorMessage("Missing geometry value");
                    return;
                }

                GH_Integer segments = new();
                if (!DA.GetData(1, ref segments))
                {
                    AddErrorMessage("Missing number of segments");
                    return;
                }

                GH_Plane direction = new();
                if (!DA.GetData(2, ref direction))
                {
                    AddErrorMessage("Missing cut plane");
                    return;
                }

                brep = surface.Value;
                numSegments = segments.Value;
                cutPlane = direction.Value;
            }

            public override void RegisterInputsParams(GH_InputParamManager pManager)
            {
                pManager.AddBrepParameter("Model", "M", "The input geometry", GH_ParamAccess.item);
                pManager.AddIntegerParameter("Segments", "S", "The number of segments to make", GH_ParamAccess.item, 5);
                pManager.AddPlaneParameter("Cut Plane", "C", "The plane to cut on, the model will be cut along the normal", GH_ParamAccess.item, Plane.WorldXY);
            }

            public override void RegisterOutputParams(GH_OutputParamManager pManager)
            {
                pManager.AddGenericParameter("Segments", "S", "The produced segments", GH_ParamAccess.list);
                pManager.AddBrepParameter("PLanes", "P", "Planes used for cutting", GH_ParamAccess.list);
            }

            public override void SetData(IGH_DataAccess DA)
            {
                DA.SetDataList(0, segments.Select(x => new GH_Segment(x)));
                DA.SetDataList(1, planes);
            }
        }
    }
}
