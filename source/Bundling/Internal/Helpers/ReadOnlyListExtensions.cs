using System;
using System.Collections.Generic;
using System.Linq;

namespace Karambolo.AspNetCore.Bundling.Internal.Helpers
{
    public static class ReadOnlyListExtensions
    {
        public static IReadOnlyList<T> Modify<T>(this IReadOnlyList<T> @this, Action<List<T>> modification)
        {
            return @this.ModifyIf(true, modification);
        }

        public static IReadOnlyList<T> ModifyIf<T>(this IReadOnlyList<T> @this, bool condition, Action<List<T>> modification)
        {
            if (modification == null)
                throw new ArgumentNullException(nameof(modification));

            if (!condition)
                return @this;

            var result = new List<T>(@this ?? Enumerable.Empty<T>());
            modification(result);
            return result;
        }
    }
}
