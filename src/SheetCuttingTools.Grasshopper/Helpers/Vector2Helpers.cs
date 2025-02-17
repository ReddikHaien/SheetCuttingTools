using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Grasshopper.Helpers
{
    internal static class Vector2Helpers
    {
        public static Point3f ToPoint3f(this Vector2 vector, float z = 0.0f)
            => new(vector.X, vector.Y, z);

        public static Point3d ToRhinoPoint3d(this g3.Vector2d vector, double z = 0.0)
            => new(vector.x, vector.y, z);
    }
}
