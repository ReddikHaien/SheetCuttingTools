using SheetCuttingTools.Abstractions.Models;

namespace SheetCuttingTools.Infrastructure.Math
{
    public static class ArrayTransform
    {
        /// <summary>
        /// Converts an unoreded array of edges into consecutives lines of points.
        /// </summary>
        /// <param name="edgeList"></param>
        /// <returns></returns>
        public static int[][] CreateEdgeLoops(Edge[] edgeList)
        {
            List<Edge> boundEdges = [.. edgeList];

            List<int[]> bounds = [];
            while (boundEdges.Count > 0)
            {
                List<int> curBound = [];
                bool found = true;
                while (found)
                {
                    found = false;
                    for (int i = 0; i < boundEdges.Count; i++)
                    {
                        var edge = boundEdges[i];

                        if (curBound.Count == 0)
                        {
                            curBound.Add(edge.A);
                            curBound.Add(edge.B);
                        }
                        else if (curBound[^1] == edge.A)
                        {
                            curBound.Add(edge.B);
                        }
                        else if (curBound[^1] == edge.B)
                        {
                            curBound.Add(edge.A);
                        }
                        else if (curBound[0] == edge.A)
                        {
                            curBound.Insert(0, edge.B);
                        }
                        else if (curBound[0] == edge.B)
                        {
                            curBound.Insert(0, edge.A);
                        }
                        else
                        {
                            continue;
                        }

                        found = true;
                        boundEdges.RemoveAt(i--);
                    }
                }
                if (curBound[0] == curBound[^1])
                {
                    curBound.RemoveAt(curBound.Count - 1);
                }
                bounds.Add([.. curBound]);
            }

            return [.. bounds];
        }

        /// <summary>
        /// Edge arrays produced by <see cref="SurfacePolygon"/> always follows the structure [(a, b), (b, c), (c, d), (d, a)]. This method rotates the array so that <paramref name="firstElement"/> is the first element, and the original structure is preserved.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This will mutate the array in place to prevent too many reallocation.
        /// </para>
        /// </remarks>
        /// <param name="edgeArray"></param>
        /// <param name="firstElement"></param>
        /// <returns>The incoming array with the elements reoriented</returns>
        public static Edge[] RotateEdgeArray(Edge[] array, Edge firstElement)
        {
            var index = Array.IndexOf(array, firstElement);
            if (index < 0) // no point rotating if the edge is not present or already the first element.
                return array;

            var edgeInArray = array[index];
            if (!Edge.Identical(edgeInArray, firstElement))
            {
                //the array represents [(a, b), (b, c), (c, d), (d, a)]. but the edge we're checking against has a different direction (e.g. (b, a) or (d, c)). We need to flip the direction of the array.
                //This can be done by reversing the array, creating
                // [(d, a), (c, d), (b, c), (a, b)]
                //And then reversing the edges, creating our flipped edge array
                // [(a, d), (d, c), (c, b), (b, a)]
                // The new index should then be arr.length - index - 1
                // e.g an array with length 5:
                // prev: 0, new: 4
                // prev: 1, new: 3
                // prev: 2, new: 2
                // prev: 3, new: 1
                // prev: 4, new: 0
                Array.Reverse(array);
                int l = array.Length;
                for (int i = 0; i < l; i++)
                    array[i] = new(array[i].B, array[i].A);
            
                index = array.Length - index - 1;
            }

            return [.. array[index..], .. array[..index]];
        }


        /// <summary>
        /// Oritents an edge array so that <paramref name="array"/>[0].A is <paramref name="firstValue"/> and <paramref name="array"/>[^1].B is <paramref name="lastValue"/>.
        /// </summary>
        /// <param name="array">The array to orient, must be a valid edge array</param>
        /// <param name="firstValue">The leading edge value.</param>
        /// <param name="lastValue">The trailing edge value.</param>
        /// <returns></returns>
        public static Edge[] GetEdgeArrayInBounds(Edge[] array, int firstValue, int lastValue)
        {
            var first = Array.FindIndex(array, e => e.A == firstValue || e.B == firstValue);

            if (first < 0)
                return [];

            var edge = array[first];

            if (edge.B == firstValue)
            {
                ReverseEdgeArray(array);
                first = array.Length - first - 1;
                edge = array[first];
            }

            var last = Array.FindIndex(array, e => e.B == lastValue);

            var segLength = last < first
                ? array.Length + last - first + 1
                : last - first + 1;

            var result = new Edge[segLength];

            for (int i = 0, k = first; i < segLength; i++, k = (k + 1) % array.Length)
            {
                result[i] = array[k];
            }
            
            return result;
        }

        /// <summary>
        /// Reverses an edge array. This mutates the array in place.
        /// </summary>
        /// <param name="array">The array to reverse</param>
        /// <returns>The same array, but reversed.</returns>
        public static Edge[] ReverseEdgeArray(Edge[] array)
        {
            Array.Reverse(array);
            int l = array.Length;
            for (int i = 0; i < l; i++)
            {
                array[i] = new(array[i].B, array[i].A);
            }

            return array;
        }
    }
}
