using Rhino.Geometry;
using Rhino.Render.Fields;
using SheetCuttingTools.Abstractions.Contracts;
using SheetCuttingTools.Abstractions.Models;
using SheetCuttingTools.Grasshopper.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Grasshopper.Models.Internal
{
    internal class MeshSegment : IGeometryProvider
    {
        private readonly Mesh mesh;

        public Mesh Mesh => mesh;

        public MeshSegment(Mesh mesh)
        {
            this.mesh = mesh;

            Vertices = new Point3fMapper(mesh.Vertices);
            Normals = new Vector3fMapper(mesh.Normals);
            Polygons = mesh.GetNgonAndFacesEnumerable().Select(x => new Polygon(x.BoundaryVertexIndexList().Select(x => (int)x).ToArray())).ToArray().AsReadOnly();

            Center = Vertices.Aggregate(Vector3.Add) / Vertices.Count;
        }

        public IReadOnlyList<Polygon> Polygons { get; }

        public IReadOnlyList<Vector3> Vertices { get; }

        public IReadOnlyList<Vector3> Normals { get; }

        public Vector3 Center { get; set; }
    }

    internal readonly struct Vector3fMapper(IReadOnlyList<Vector3f> points) : IReadOnlyList<Vector3>
    {
        public Vector3 this[int index]
        {
            get => points[index].ToVector3();
        }

        public int Count => points.Count;

        public IEnumerator<Vector3> GetEnumerator()
            => points.Select(x => x.ToVector3()).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }

    internal readonly struct Point3fMapper(IReadOnlyList<Point3f> points) : IReadOnlyList<Vector3>
    {
        public Vector3 this[int index]
        {
            get => points[index].ToVector3();
        }

        public int Count => points.Count;

        public IEnumerator<Vector3> GetEnumerator()
            => points.Select(x => x.ToVector3()).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }
}
