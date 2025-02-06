using Rhino.Geometry;
using SheetCuttingTools.Abstractions.Contracts;
using SheetCuttingTools.Grasshopper.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Grasshopper.Models
{
    /// <summary>
    /// Simple wrapper class around <see cref="Surface"/> objects
    /// </summary>
    public class SurfaceWrapper(Surface surface) : ISurfaceProvider
    {
        private readonly Surface surface = surface;

        public Vector3 Center { get; } = surface.GetBoundingBox(true).Center.ToVector3();

        public Vector3 NormalAt(Vector3 point)
        {
            (double u, double v) = GetPointAtSurface(point);
            return surface.NormalAt(u, v).ToVector3();
        }

        public Vector3 PointAt(Vector3 point)
        {
            (double u, double v) = GetPointAtSurface(point);
            return surface.PointAt(u, v).ToVector3();
        }

        private (double U, double V) GetPointAtSurface(Vector3 point)
        {
            if (!surface.ClosestPoint(point.ToPoint3d(), out var u, out var v))
                throw new InvalidOperationException("Failed to find point at surface");

            return (u, v);
        }
    }
}
