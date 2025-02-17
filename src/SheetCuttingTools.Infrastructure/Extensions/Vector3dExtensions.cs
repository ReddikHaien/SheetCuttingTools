using g3;

namespace SheetCuttingTools.Infrastructure.Extensions;

public static class Vector3dExtensions
{
    public static Vector3d Min(this Vector3d a, Vector3d b)
        => new(System.Math.Min(a.x, b.x), System.Math.Min(a.y, b.y), System.Math.Min(a.z, b.z));

    public static Vector3d Max(this Vector3d a, Vector3d b)
        => new(System.Math.Max(a.x, b.x), System.Math.Max(a.y, b.y), System.Math.Max(a.z, b.z));
}
