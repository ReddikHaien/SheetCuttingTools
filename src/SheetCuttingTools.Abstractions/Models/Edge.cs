using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Abstractions.Models
{
    /// <summary>
    /// Represents an edge in a model. An edge represents a connection between two points indexed by <see cref="A"/> and <see cref="B"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Given two edges <c>[Q - P]</c> <c>[P - Q]</c>. These edges represent the same edge but with different orientations. In this implementation edge equality ignores the direction of an edge, meaning that
    /// <see cref="Edge.Equals(Edge)"/> returns true for <c>[Q - P]</c> <c>[P - Q]</c>. In cases where direction needs to be taken into account <see cref="Identical(Edge, Edge)"/> can be used.
    /// </para>
    /// </remarks>
    /// <param name="a">The first point in the edge.</param>
    /// <param name="b">The second point in the edge.</param>
    public readonly struct Edge(int a, int b) : IEquatable<Edge>
    {
        /// <summary>
        /// The first point in the edge.
        /// </summary>
        public int A { get; } = a;

        /// <summary>
        /// The second point in the edge.
        /// </summary>
        public int B { get; } = b;

        public override int GetHashCode()
            => A < B
            ? HashCode.Combine(A, B) 
            : HashCode.Combine(B, A);

        public override bool Equals([NotNullWhen(true)] object? obj)
            => obj is Edge edge && Equals(edge);

        public bool Equals(Edge other)
            => other.A == A && other.B == B
            || other.B == A && other.A == B;

        public static bool operator ==(Edge left, Edge right)
            => left.Equals(right);

        public static bool operator !=(Edge left, Edge right)
            => !(left==right);

        public bool ContainsPoint(int point)
            => point == A || point == B;

        public int OtherPoint(int point)
            => point == A ? B : A;


        /// <summary>
        /// Checks if two edges are the same edge and have the same direction.
        /// </summary>
        /// <param name="a">an edge to test</param>
        /// <param name="b">the other edge to test.</param>
        /// <returns><see langword="true"/> if edge <paramref name="a"/> and <paramref name="b"/> have the same values and the same direction.</returns>
        public static bool Identical(Edge a, Edge b)
            => a.A == b.A && a.B == b.B;

        public override string ToString()
            => A < B
            ? $"[{A} - {B}] ->"
            : $"[{B} - {A}] <-";
    }
}
