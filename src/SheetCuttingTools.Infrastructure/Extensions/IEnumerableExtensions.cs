using SheetCuttingTools.Abstractions.Behaviors;
using SheetCuttingTools.Abstractions.Contracts;
using SheetCuttingTools.Abstractions.Models;
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

        public static Polygon MaxByMany(this IEnumerable<Polygon> polygons, IReadOnlyList<IPolygonScorer> scorers, IGeometry geometry)
        {
            if (scorers.Count == 0)
                return polygons.First();

            if (scorers.Count == 1)
                return polygons.MaxBy(x => scorers[0].ScorePolygon(new(x, geometry)));

            if (scorers.Count == 2)
                return polygons.MaxBy(x => (scorers[0].ScorePolygon(new(x, geometry)), scorers[1].ScorePolygon(new(x, geometry))));

            return polygons.MaxBy(x => scorers.Average(y => y.ScorePolygon(new(x, geometry))));
        }
    }
}
