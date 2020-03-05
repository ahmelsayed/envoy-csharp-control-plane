using System;
using System.Collections.Generic;

namespace Envoy.Control.Runner
{
    public static class IEnumerableExtensions
    {
        public static IEnumerable<T> AddOrUpdateInPlace<T>(this IEnumerable<T> source, T obj, Func<T, T, bool> replace)
        {
            var updated = false;
            foreach (var item in source)
            {
                if (replace(obj, item))
                {
                    updated = true;
                    yield return obj;
                }
                else
                {
                    yield return item;
                }
            }
            if (!updated)
            {
                yield return obj;
            }
        }
    }
}