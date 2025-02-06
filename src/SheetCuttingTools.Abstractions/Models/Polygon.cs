using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Abstractions.Models
{
    public readonly struct Polygon(int[] points) : IEquatable<Polygon>
    {
        /// <summary>
        /// The point indices of this polygon
        /// </summary>
        public int[] Points { get; } = points;

        private readonly int hashCode = ((IStructuralEquatable)points).GetHashCode(EqualityComparer<int>.Default);

        public override int GetHashCode()
            => hashCode;

        public override bool Equals([NotNullWhen(true)] object? obj)
            => obj is Polygon polygon && Equals(polygon);
        
        /// <summary>
        /// Checks if <paramref name="other"/> is identical to this polygon.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(Polygon other)
            => Points.SequenceEqual(other.Points);

        public static bool operator ==(Polygon left, Polygon right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Polygon left, Polygon right)
        {
            return !(left == right);
        }

        public IEnumerable<Edge> GetEdges()
        {
            var l = Points.Length;
            for(int i = 1; i < l; i++)
            {
                var j = i - 1;
                yield return new Edge(Points[j], Points[i]);
            }

            yield return new Edge(Points[^1], Points[0]);
        }
    }
}
