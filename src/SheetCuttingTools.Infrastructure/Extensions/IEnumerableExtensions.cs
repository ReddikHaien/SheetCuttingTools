using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Infrastructure.Extensions
{
    public static class IEnumerableExtensions
    {
        public static IEnumerable<(T First, T Second)> SlidingWindow<T>(this IEnumerable<T> values)
        {
            var e = values.GetEnumerator();
            
            if(!e.MoveNext())
                yield break;

            T initial = e.Current;
            T first = initial;
            bool more = false;
            while (e.MoveNext())
            {
                more = true;
                T second = e.Current;
                yield return (first, second)!;
                first = second;
            }

            //loop
            if (more)
                yield return (first, initial);
        }

        public static (T A, T B) Aggregate<T>(this IEnumerable<T> values, Func<T, T, T> a, Func<T, T, T> b)
        {
            (T A, T B) aggregateFunc((T A, T B) first, (T A, T B) second)
            {
                return (a(first.A, second.A), b(first.B, second.B));
            }
            return Enumerable.Aggregate(values.Select(x => (x, x)), aggregateFunc);
        }
    }
}
