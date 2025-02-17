using g3;

namespace SheetCuttingTools.Infrastructure.Extensions;

public static class Vector2dExtensions
{
    public static Vector2d Min(this Vector2d a, Vector2d b)
        => new(System.Math.Min(a.x, b.x), System.Math.Min(a.y, b.y));

    public static Vector2d Max(this Vector2d a, Vector2d b)
        => new(System.Math.Max(a.x, b.x), System.Math.Max(a.y, b.y));

}
