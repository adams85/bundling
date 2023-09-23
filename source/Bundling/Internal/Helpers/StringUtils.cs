using System;
using System.Text;
using Microsoft.Extensions.Primitives;

namespace Karambolo.AspNetCore.Bundling.Internal.Helpers
{
    internal static class StringUtils
    {
        public static string RemoveQuotes(ref string value)
        {
            if (value.StartsWith("'", StringComparison.Ordinal))
                if (value.EndsWith("'", StringComparison.Ordinal))
                {
                    value = value.Substring(1, value.Length - 2);
                    return "'";
                }
                else
                    return null;

            if (value.StartsWith("\"", StringComparison.Ordinal))
                if (value.EndsWith("\"", StringComparison.Ordinal))
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
                throw new ArgumentOutOfRangeException(nameof(startIndex), startIndex, null);

            var endIndex = startIndex + count;
            if (count < 0 || length < endIndex)
                throw new ArgumentOutOfRangeException(nameof(count), count, null);

            var difference = count - segment.Length;
            if (difference > 0)
                stringBuilder.Remove(startIndex, difference);
            else
                stringBuilder.Insert(startIndex, " ", -difference);

            for (var i = 0; i < segment.Length; i++, startIndex++)
                stringBuilder[startIndex] = segment[i];

            return stringBuilder;
        }

#if !NETCOREAPP3_0_OR_GREATER
        public static ReadOnlySpan<char> AsSpan(this StringSegment segment)
        {
            return segment.Buffer.AsSpan(segment.Offset, segment.Length);
        }
#endif

#if NETCOREAPP3_0_OR_GREATER
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static string Concat(this ReadOnlySpan<char> str0, ReadOnlySpan<char> str1)
        {
#if NETCOREAPP3_0_OR_GREATER
            return string.Concat(str0, str1);
#else
            var buffer = new char[str0.Length + str1.Length];
            Span<char> span = buffer;
            str0.CopyTo(span);
            str1.CopyTo(span.Slice(str0.Length));
            return new string(buffer);
#endif
        }

#if NETCOREAPP3_0_OR_GREATER
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public static string Concat(this ReadOnlySpan<char> str0, ReadOnlySpan<char> str1, ReadOnlySpan<char> str2)
        {
#if NETCOREAPP3_0_OR_GREATER
            return string.Concat(str0, str1, str2);
#else
            var buffer = new char[str0.Length + str1.Length + str2.Length];
            Span<char> span = buffer;
            str0.CopyTo(span);
            str1.CopyTo(span = span.Slice(str0.Length));
            str2.CopyTo(span.Slice(str1.Length));
            return new string(buffer);
#endif
        }

        public static byte[] GetBytesWithPreamble(this Encoding encoding, string value)
        {
            var bytes = encoding.GetBytes(value);

            var preamble = encoding.GetPreamble();
            if (preamble.Length > 0)
            {
                var contentBytes = bytes;
                Span<byte> bytesSpan = bytes = new byte[preamble.Length + contentBytes.Length];
                preamble.CopyTo(bytesSpan);
                contentBytes.CopyTo(bytesSpan.Slice(preamble.Length));
            }

            return bytes;
        }
    }
}
