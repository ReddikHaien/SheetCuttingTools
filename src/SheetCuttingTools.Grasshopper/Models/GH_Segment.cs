using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Display;
using Rhino.Geometry;
using SheetCuttingTools.Abstractions.Contracts;
using SheetCuttingTools.Grasshopper.Helpers;
using SheetCuttingTools.Grasshopper.Models.Internal;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace SheetCuttingTools.Grasshopper.Models
{
    public class GH_Segment : GH_Goo<IGeometryProvider>, IGH_PreviewData
    {
        private readonly Lazy<Displayable> mesh;

        private GH_Segment(IGeometryProvider segment, Func<Displayable> meshmaker) : base(segment)
        {
            mesh = new(meshmaker, System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);
        }

        public GH_Segment(IGeometryProvider segment) : this(segment, () => CreateMesh(segment))
        {
        }

        private Displayable Mesh => mesh.Value;

        public BoundingBox ClippingBox => Mesh.GetBoundingBox();

        public override bool IsValid => Mesh.IsValid();

        public override string TypeName => "Segment";

        public override string TypeDescription => "A segment produced by one or more segmentors";

        public override IGH_Goo Duplicate()
            => new GH_Segment(Value, () => Mesh);

        public override string ToString()
            => "Segment";

        public void DrawViewportWires(GH_PreviewWireArgs args)
        {
        }

        public void DrawViewportMeshes(GH_PreviewMeshArgs args)
            => Mesh.DrawMesh(args);

        private static Displayable CreateMesh(IGeometryProvider segment)
        {
            if (segment is MeshSegment meshSegment)
            {
                return new Displayable.DisplayableMesh(meshSegment.Mesh);
            }

            if (segment is BrepSegment brepSegment)
                return new Displayable.DisplayableBrep(brepSegment.Brep);

            var mesh = new Mesh();

            List<Point3f> verts = [];
            List<Vector3f> normals = [];
            List<Color> colors = [];
            List<MeshFace> faces = [];

            Dictionary<int, int> mapped = [];

            foreach (var polygon in segment.Polygons)
            {
                int[] i = polygon.Points.Select(x =>
                {
                    if (!mapped.TryGetValue(x, out int j))
                    {
                        verts.Add((segment as IGeometryProvider).Vertices[x].ToPoint3f());
                        normals.Add((segment as IGeometryProvider).Normals[x].ToVector3f());
                        return verts.Count - 1;
                    }
                    return j;
                }).ToArray();

                colors.AddRange(i.Select(_ => ColorHelper.GetColor(segment.GetHashCode())));

                switch (i.Length)
                {
                    case 3:
                        faces.Add(new(i[0], i[1], i[2]));
                        continue;

                    case 4:
                        faces.Add(new(i[0], i[1], i[2], i[3]));
                        break;

                    default:
                        List<int> points = [.. i];
                        while (points.Count > 3)
                        {
                            int l = points.Count;
                            for (int ia = 0; ia < l; ia++)
                            {
                                int ib = (ia + 1) % l;
                                int ic = (ia + 2) % l;

                                var triangle = new Triangle3d(
                                    verts[points[ia]],
                                    verts[points[ib]],
                                    verts[points[ic]]
                                );

                                bool failed = false;

                                for (int k = 2, o = (ic + 1) % l; k < l; k++, o = (o + 1) % l)
                                {
                                    var p = verts[points[o]];
                                    var bary = triangle.BarycentricCoordsAt(p, out var _);

                                    if (bary.X > 0 && bary.Y > 0)
                                    {
                                        failed = true;
                                        break;
                                    }
                                }

                                if (!failed)
                                {
                                    faces.Add(new(points[ia], points[ib], points[ic]));
                                    points.RemoveAt(ib);
                                    break;
                                }
                            }
                        }

                        faces.Add(new(points[0], points[1], points[2]));

                        break;
                }
            }

            mesh.Vertices.AddVertices(verts);
            mesh.Faces.AddFaces(faces);
            colors.ForEach(x => mesh.VertexColors.Add(x));
            normals.ForEach(x => mesh.Normals.Add(x));

            return new Displayable.DisplayableMesh(mesh);
        }
    }
    internal abstract record Displayable
    {
        public abstract void DrawMesh(GH_PreviewMeshArgs args);

        public abstract BoundingBox GetBoundingBox();

        public abstract bool IsValid();

        public record DisplayableMesh(Mesh Mesh) : Displayable
        {
            public override void DrawMesh(GH_PreviewMeshArgs args)
            {
                args.Pipeline.DrawMeshFalseColors(Mesh);
            }

            public override BoundingBox GetBoundingBox()
                => Mesh.GetBoundingBox(true);

            public override bool IsValid()
                => Mesh.IsValid;
        }

        public record DisplayableBrep(Brep Brep) : Displayable
        {
            public override void DrawMesh(GH_PreviewMeshArgs args)
            {
                args.Pipeline.DrawBrepShaded(Brep, new DisplayMaterial
                {
                    Diffuse = ColorHelper.GetColor(Brep.GetHashCode())
                });
            }

            public override BoundingBox GetBoundingBox()
                => Brep.GetBoundingBox(true);

            public override bool IsValid()
                => Brep.IsValid;
        }
    }

}
