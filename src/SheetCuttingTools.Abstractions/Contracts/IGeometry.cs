using g3;
using SheetCuttingTools.Abstractions.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Abstractions.Contracts
{
    /// <summary>
    /// Base geometry provider.
    /// </summary>
    public interface IGeometry
    {
        /// <summary>
        /// The polygons of this geometry.
        /// </summary>
        public IReadOnlyList<Polygon> Polygons { get; }

        /// <summary>
        /// The vertices of this geometry.
        /// </summary>
        public IReadOnlyList<Vector3d> Vertices { get; }

        /// <summary>
        /// The normals of this geometry.
        /// </summary>
        public IReadOnlyList<Vector3f> Normals { get; }

        /// <summary>
        /// The parent geometry.
        /// </summary>
        public IGeometry? Parent { get; }

        /// <summary>
        /// The center of the geometry
        /// </summary>
        public Vector3d Center3d { get; }
    }
}
