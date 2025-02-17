using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Grasshopper.Helpers
{
    internal static class Vector3Extensions
    {
        public static Vector3 ToVector3(this Vector3f vector)
            => new(vector.X, vector.Y, vector.Z);

        public static Vector3 ToVector3(this Vector3d vector)
            => new((float)vector.X, (float)vector.Y, (float)vector.Z);

        public static Vector3 ToVector3(this Point3f point)
            => new(point.X, point.Y, point.Z);

        public static g3.Vector3d ToG3Vector3d(this Point3f point)
            => new g3.Vector3d(point.X, point.Y, point.Z);

        public static g3.Vector3f ToG3Vector3f(this Vector3f point)
            => new g3.Vector3f(point.X, point.Y, point.Z);

        public static Vector3 ToVector3(this Point3d point)
            => new((float)point.X, (float)point.Y, (float) point.Z);

        public static g3.Vector3d ToG3Vector3d(this Point3d point)
            => new g3.Vector3d(point.X, point.Y, point.Z);

        public static Point3d ToPoint3d(this Vector3 point)
            => new(point.X, point.Y, point.Z);

        public static Point3f ToPoint3f(this Vector3 point)
            => new(point.X, point.Y, point.Z);

        public static Point3f ToPoint3f(this g3.Vector3d point)
            => new((float)point.x, (float)point.y, (float)point.z);

        public static Vector3f ToVector3f(this Vector3 vector)
            => new(vector.X, vector.Y, vector.Z);

        public static Vector3f ToVector3f(this g3.Vector3f vector)
            => new(vector.x, vector.y, vector.z);
    }
}
