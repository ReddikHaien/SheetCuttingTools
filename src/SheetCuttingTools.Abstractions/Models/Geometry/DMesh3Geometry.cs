using g3;
using SheetCuttingTools.Abstractions.Contracts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Abstractions.Models.Geometry
{
    public class DMesh3Geometry(DMesh3 mesh, Polygon[] polygons) : IGeometry, IHaveMesh
    {
        private readonly DMesh3 mesh = mesh;
        public DMesh3 Mesh => mesh;

        public IReadOnlyList<Polygon> Polygons { get; } = polygons.AsReadOnly();

        public IReadOnlyList<Vector3d> Vertices { get; } = new VerticesReader(mesh);

        public IReadOnlyList<Vector3f> Normals { get; } = new NormalsReader(mesh);

        public IGeometry? Parent { get; init; }

        public Vector3d Center3d { get; } = mesh.GetBounds().Center;
    }

    internal readonly struct NormalsReader(DMesh3 mesh) : IReadOnlyList<Vector3f>
    {
        private readonly DMesh3 mesh = mesh;

        public Vector3f this[int index] => mesh.GetVertexNormal(index);

        public int Count => mesh.VertexCount;

        public IEnumerator<Vector3f> GetEnumerator()
        {
            var m = mesh;
            return (mesh.HasVertexNormals ? Enumerable.Range(0, Count).Select(m.GetVertexNormal) : []).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }

    internal readonly struct VerticesReader(DMesh3 mesh) : IReadOnlyList<Vector3d>
    {
        public Vector3d this[int index] => mesh.GetVertex(index);

        public int Count => mesh.VertexCount;

        public IEnumerator<Vector3d> GetEnumerator()
            => mesh.Vertices().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }
}
