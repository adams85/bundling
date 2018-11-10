using System;
using System.Collections.Generic;
using System.Linq;

namespace Karambolo.AspNetCore.Bundling.Internal.Helpers
{
    public static class ReadOnlyListExtensions
    {
        public static IReadOnlyList<T> Modify<T>(this IReadOnlyList<T> list, Action<List<T>> modification)
        {
            return list.ModifyIf(true, modification);
        }

        public static IReadOnlyList<T> ModifyIf<T>(this IReadOnlyList<T> list, bool condition, Action<List<T>> modification)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (modification == null)
                throw new ArgumentNullException(nameof(modification));

            if (!condition)
                return list;

            var result = new List<T>(list ?? Enumerable.Empty<T>());
            modification(result);
            return result;
        }
    }
}
