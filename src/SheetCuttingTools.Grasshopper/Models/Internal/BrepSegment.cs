using Rhino.Geometry;
using SheetCuttingTools.Abstractions.Contracts;
using SheetCuttingTools.Abstractions.Models;
using SheetCuttingTools.Grasshopper.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Grasshopper.Models.Internal
{
    internal class BrepSegment(Brep brep) : ISurfaceProvider, IGeometryProvider
    {
        private readonly Brep brep = brep;

        private readonly Lazy<IGeometryProvider> polygonized = new Lazy<IGeometryProvider>(() =>
        {
            var created = Mesh.CreateFromBrep(brep, MeshingParameters.QualityRenderMesh);
            var combined = new Mesh();

            foreach(var m in created)
            {
                combined.Append(m);
            }
            combined.Weld(Math.PI*2);
            return combined.CreateGeometryProvider();
            
        }, System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

        public Vector3 Center { get; set; } = brep.GetBoundingBox(true).Center.ToVector3();

        public IReadOnlyList<Polygon> Polygons => polygonized.Value.Polygons;

        public IReadOnlyList<Vector3> Vertices => polygonized.Value.Vertices;

        public IReadOnlyList<Vector3> Normals => polygonized.Value.Normals;

        public Brep Brep => brep;

        public Vector3 NormalAt(Vector3 point)
        {
            var p = PointAt(point).ToPoint3d();
            foreach(var face in brep.Faces)
            {
                if (!face.ClosestPoint(p, out var x, out var y))
                {
                    return face.NormalAt(x, y).ToVector3();
                }
            }

            return Vector3.Zero;
        }

        public Vector3 PointAt(Vector3 point)
            => brep.ClosestPoint(point.ToPoint3d()).ToVector3();
    }
}
