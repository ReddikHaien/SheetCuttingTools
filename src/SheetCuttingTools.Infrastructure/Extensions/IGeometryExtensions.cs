using g3;
using SheetCuttingTools.Abstractions.Contracts;
using SheetCuttingTools.Abstractions.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Infrastructure.Extensions
{
    public static class IGeometryExtensions
    {
        public static (Vector3d A, Vector3d B) GetVertices(this IGeometry geometry, Edge edge)
        {
            return (geometry.Vertices[edge.A], geometry.Vertices[edge.B]);
        }

        public static DMesh3 ConvertToDMesh3(this IGeometry geometry)
        {
            if (geometry is IHaveMesh meshGeometry)
                return meshGeometry.Mesh;

            throw new NotImplementedException("geometry to dmesh not implemented");
        }
    }
}
