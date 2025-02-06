using Grasshopper.Kernel;
using Rhino.Geometry;
using SheetCuttingTools.Abstractions.Contracts;
using SheetCuttingTools.Abstractions.Models;
using SheetCuttingTools.Grasshopper.Helpers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Threading;

namespace SheetCuttingTools.Grasshopper.Components.Segmentation
{
    public abstract class BaseSegmentator(string name, string nickname, string description) : BaseToolComponent(name, nickname, description, Constants.Category, Constants.SegmentationCategory)
    {
        //protected Dictionary<int, Mesh> previewMesh = [];
        //public override BoundingBox ClippingBox
        //{
        //    get
        //    {
        //        BoundingBox b = default;
        //        foreach (var m in previewMesh.Values)
        //            b.Union(m.GetBoundingBox(true));

        //        return b;
        //    }
        //}

        //public override void DrawViewportMeshes(IGH_PreviewArgs args)
        //{
        //    if (args.Document.PreviewMode != GH_PreviewMode.Shaded || !args.Display.SupportsShading || previewMesh.Count == 0)
        //    {
        //        return;
        //    }
        //    foreach (var m in previewMesh.Values)
        //        args.Display.DrawMeshFalseColors(m);
        //}

        //protected void UpdatePreviewMesh(IGH_DataAccess DA, Segment[] segments, CancellationToken cancellationToken)
        //{
        //    if (segments is null)
        //        return;

        //    try
        //    {
        //        var mesh = new Mesh();

        //        List<Point3f> verts = [];
        //        List<Vector3f> normals = [];
        //        List<Color> colors = [];
        //        List<MeshFace> faces = [];

        //        foreach (var (index, segment) in segments.Select((x, i) => (i, x)))
        //        {
        //            cancellationToken.ThrowIfCancellationRequested();
        //            int count = verts.Count;
        //            List<Vector3> v = [];
        //            List<Vector3> n = [];
        //            List<Color> c = [];
        //            List<MeshFace> f = [];

        //            Dictionary<int, int> mapped = [];

        //            foreach (var polygon in segment.Polygons)
        //            {
        //                int[] i = polygon.Points.Select(x =>
        //                {
        //                    if (!mapped.TryGetValue(x, out int j))
        //                    {
        //                        v.Add((segment as IGeometryProvider).Vertices[x]);
        //                        n.Add((segment as IGeometryProvider).Normals[x]);
        //                        return v.Count - 1 + count;
        //                    }
        //                    return j + count;
        //                }).ToArray();

        //                c.AddRange(i.Select(_ => ColorHelper.GetColor(index)));

        //                switch (i.Length)
        //                {
        //                    case 3:
        //                        f.Add(new(i[0], i[1], i[2]));
        //                        continue;

        //                    case 4:
        //                        f.Add(new(i[0], i[1], i[2], i[3]));
        //                        break;

        //                    default:
        //                        List<int> points = [.. i];
        //                        while (points.Count > 3)
        //                        {
        //                            int l = points.Count;
        //                            for (int ia = 0; ia < l; ia++)
        //                            {
        //                                int ib = (ia + 1) % l;
        //                                int ic = (ia + 2) % l;

        //                                var triangle = new Triangle3d(
        //                                    v[points[ia]-count].ToPoint3d(),
        //                                    v[points[ib]-count].ToPoint3d(),
        //                                    v[points[ic]-count].ToPoint3d()
        //                                );


        //                                bool failed = false;

        //                                for (int k = 2, o = (ic + 1) % l; k < l; k++, o = (o + 1) % l)
        //                                {
        //                                    var p = v[points[o] - count];
        //                                    var bary = triangle.BarycentricCoordsAt(p.ToPoint3d(), out var _);

        //                                    if (bary.X > 0 && bary.Y > 0)
        //                                    {
        //                                        failed = true;
        //                                        break;
        //                                    }
        //                                }

        //                                if (!failed)
        //                                {
        //                                    f.Add(new(points[ia], points[ib], points[ic]));
        //                                    points.RemoveAt(ib);
        //                                    break;
        //                                }
        //                            }
        //                        }

        //                        f.Add(new(points[0], points[1], points[2]));

        //                        break;
        //                }
        //            }

        //            verts.AddRange(v.Select(x => x.ToPoint3f()));
        //            normals.AddRange(n.Select(x => x.ToVector3f()));
        //            colors.AddRange(c);
        //            faces.AddRange(f);
        //        }

        //        mesh.Vertices.AddVertices(verts);
        //        mesh.Faces.AddFaces(faces);
        //        colors.ForEach(x => mesh.VertexColors.Add(x));
        //        normals.ForEach(x => mesh.Normals.Add(x));

        //        previewMesh.Add(DA.Iteration, mesh);
        //    }
        //    catch (Exception e)
        //    {
        //        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Failed to create preview: {e.Message}");
        //    }
        //}
    }
}
