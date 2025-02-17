using System.Diagnostics;
using System.Numerics;

namespace SheetCuttingTools.Abstractions.Models.Numerics
{
    public readonly struct HighPresVector2(double x, double y)
    {
        public static readonly HighPresVector2 Zero = new(0, 0);

        public double X { get; } = x;
        public double Y { get; } = y;

        public double Length => Math.Sqrt(X * X + Y * Y);

        public static HighPresVector2 operator -(HighPresVector2 vector)
            => new(-vector.X, -vector.Y);

        public static HighPresVector2 operator -(HighPresVector2 left, HighPresVector2 right)
            => new(left.X - right.X, left.Y - right.Y);

        public static HighPresVector2 operator +(HighPresVector2 left, HighPresVector2 right)
            => new(left.X + right.X, left.Y + right.Y);

        public static HighPresVector2 operator *(double left, HighPresVector2 right)
            => new(right.X * left, right.Y * left);

        public static HighPresVector2 operator *(HighPresVector2 left, double right)
            => new(left.X * right, left.Y * right);

        public static HighPresVector2 operator /(HighPresVector2 left, double right)
            => new(left.X / right, left.Y / right);

        public static double Dot(HighPresVector2 left, HighPresVector2 right)
            => left.X * right.X + left.Y * right.Y;

        public static implicit operator Vector2(HighPresVector2 v)
            => new((float)v.X, (float)v.Y);

        public static bool EpsilonEquals(HighPresVector2 a, HighPresVector2 b)
            => DistanceSquared(a, b) < 0.001;

        public static bool IsNaN(HighPresVector2 vector)
            => double.IsNaN(vector.X) || double.IsNaN(vector.Y);

        public static double DistanceSquared(HighPresVector2 left, HighPresVector2 right)
        {
            var x = (left.X - right.X);
            var y = (left.Y - right.Y);
            return x * x + y * y;
        }

        public static double Distance(HighPresVector2 left, HighPresVector2 right)
            => Math.Sqrt(DistanceSquared(left, right));

        public static HighPresVector2 Normalize(HighPresVector2 vector)
        {
            var l = vector.Length;
            return new(vector.X / l, vector.Y / l);
        }

        public static HighPresVector2 Min(HighPresVector2 a, HighPresVector2 b)
            => new HighPresVector2(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y));

        public static HighPresVector2 Max(HighPresVector2 a, HighPresVector2 b)
            => new HighPresVector2(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y));

        public override string ToString()
            => $"{X}, {Y}";
    }
}
