using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Infrastructure.Extensions
{
    public static class CollectionExtensions
    {
        public static IReadOnlyList<T> Combine<T>(this IReadOnlyList<T> first, IReadOnlyList<T> second)
            => new CombinedReadonlyLists<T>(first, second);


    }

    public class CombinedReadonlyLists<T>(IReadOnlyList<T> first, IReadOnlyList<T> second) : IReadOnlyList<T>
    {
        private readonly IReadOnlyList<T> first = first;
        private readonly IReadOnlyList<T> second = second;

        public T this[int index] => index < first.Count ? first[index] : second[index - first.Count];

        public int Count => first.Count + second.Count;

        public IEnumerator<T> GetEnumerator()
            => first.Concat(second).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }
}
