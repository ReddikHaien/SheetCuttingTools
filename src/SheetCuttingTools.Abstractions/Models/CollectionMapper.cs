using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Abstractions.Models
{
    /// <summary>
    /// Wrapper class for accessing a collection of type <typeparamref name="U"/> as if it was a collection of <see cref="T"/>.
    /// </summary>
    /// <typeparam name="U">The inner type.</typeparam>
    /// <typeparam name="T">The exposed type.</typeparam>
    /// <param name="inner">The collection.</param>
    /// <param name="mapper">The mapping function to use.</param>
    public class CollectionMapper<U, T>(IReadOnlyList<U> inner, Func<U, T> mapper) : IReadOnlyList<T>
    {
        private readonly IReadOnlyList<U> inner = inner;
        private readonly Func<U, T> mapper = mapper;

        public T this[int index] => mapper(inner[index]);

        public int Count => inner.Count;

        public IEnumerator<T> GetEnumerator()
            => inner.Select(mapper).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }
}
