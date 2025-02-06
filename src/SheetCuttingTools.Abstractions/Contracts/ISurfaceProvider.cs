using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Abstractions.Contracts
{
    /// <summary>
    /// Provides information about a surface
    /// </summary>
    public interface ISurfaceProvider
    {
        /// <summary>
        /// Returns the closest point to the surface at the specified location.
        /// </summary>
        /// <param name="point">The target point.</param>
        /// <returns></returns>
        Vector3 PointAt(Vector3 point);

        /// <summary>
        /// Returns the normal at the closest point to the surface at the specified location.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        Vector3 NormalAt(Vector3 point);


        /// <summary>
        /// The center of the surface.
        /// </summary>
        Vector3 Center { get; }
    }
}
