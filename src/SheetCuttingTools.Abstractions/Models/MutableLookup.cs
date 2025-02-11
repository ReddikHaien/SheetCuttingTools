using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Abstractions.Models
{
    /// <summary>
    /// Helper class for managing a mutable lookup.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TElement">The element type.</typeparam>
    public class MutableLookup<TKey, TElement>(ILookup<TKey, TElement> lookup)
        where TKey : notnull
    {

        public MutableLookup() : this(Array.Empty<(TKey, TElement)>().ToLookup(x => x.Item1, x => x.Item2))
        {

        }

        private readonly Dictionary<TKey, List<TElement>> dict = lookup.ToDictionary(x => x.Key, x => x.ToList());
        public IReadOnlyList<TElement> this[TKey key]
        {
            get => dict.TryGetValue(key, out var x) ? x : [];
            set => dict[key] = [.. value];
        }

        public void Add(TKey key, TElement value)
        {
            if (!dict.TryGetValue(key, out List<TElement>? list))
            {
                list = [value];
                dict.Add(key, list);
                return;
            }
            list.Add(value);
        }

        public void RemoveElement(TKey key, TElement value)
        {
            if (!dict.TryGetValue(key, out List<TElement>? list))
                return;

            list.Remove(value);

            if (list.Count == 0)
            {
                dict.Remove(key);
            }
        }
    }
}
