using System;
using System.Text;
using Microsoft.Extensions.Primitives;

namespace Karambolo.AspNetCore.Bundling.Internal.Helpers
{
    internal static class StringUtils
    {
        public static string RemoveQuotes(ref string value)
        {
            if (value.StartsWith("'"))
                if (value.EndsWith("'"))
                {
                    value = value.Substring(1, value.Length - 2);
                    return "'";
                }
                else
                    return null;

            if (value.StartsWith("\""))
                if (value.EndsWith("\""))
                {
                    value = value.Substring(1, value.Length - 2);
                    return "\"";
                }
                else
                    return null;

            return string.Empty;
        }

        public static StringBuilder Substitute(this StringBuilder stringBuilder, int startIndex, int count, in StringSegment segment)
        {
            if (stringBuilder == null)
                throw new ArgumentNullException(nameof(stringBuilder));

            var length = @stringBuilder.Length;
            if (startIndex < 0 || length < startIndex)
                throw new ArgumentOutOfRangeException(nameof(startIndex));

            var endIndex = startIndex + count;
            if (count < 0 || length < endIndex)
                throw new ArgumentOutOfRangeException(nameof(count));

            var difference = count - segment.Length;
            if (difference > 0)
                stringBuilder.Remove(startIndex, difference);
            else
                stringBuilder.Insert(startIndex, " ", -difference);

            for (var i = 0; i < segment.Length; i++, startIndex++)
                stringBuilder[startIndex] = segment[i];

            return stringBuilder;
        }
    }
}
