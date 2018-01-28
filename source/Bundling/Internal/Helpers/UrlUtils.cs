using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

namespace Karambolo.AspNetCore.Bundling.Internal.Helpers
{
    static class UrlUtils
    {
        const string hexChars = "0123456789abcdef";

        static readonly char[] illegalFileNameChars = Path.GetInvalidPathChars().Concat(Path.GetInvalidFileNameChars()).ToArray();

        readonly static Uri dummyBaseUri = new Uri("http://host");

        public static void FromRelative(string url, out PathString path, out QueryString query, out FragmentString fragment)
        {
            var uri = new Uri(dummyBaseUri, url);
            UriHelper.FromAbsolute(uri.ToString(), out string scheme, out HostString host, out path, out query, out fragment);
        }

        public static QueryString NormalizeQuery(QueryString query, out IDictionary<string, StringValues> parsedQuery)
        {
            if (!query.HasValue)
            {
                parsedQuery = null;
                return query;
            }

            var parsed = QueryHelpers.ParseQuery(query.ToString());

            var builder = new QueryBuilder();
            foreach (var kvp in parsed.OrderBy(kvp => kvp.Key))
            {
                var n = kvp.Value.Count;
                for (var i = 0; i < n; i++)
                {
                    var value = kvp.Value[i];
                    builder.Add(kvp.Key, value);
                }
            }

            parsedQuery = parsed;
            return builder.ToQueryString();
        }

        public static string NormalizePath(string path)
        {
            return path.StartsWith("/") ? path : '/' + path;
        }

        public static string GetFileName(string path, out string basePath)
        {
            var index = path.LastIndexOf('/') + 1;

            basePath = path.Substring(0, index);
            return path.Substring(index);
        }

        public static string PathToFileName(string value)
        {
            var chars = value.ToCharArray();

            char c;
            var n = chars.Length;
            for (var i = 0; i < n; i++)
                chars[i] = Array.IndexOf(illegalFileNameChars, c = chars[i]) < 0 ? char.ToLowerInvariant(c) : '_';

            return new string(chars);
        }

        public static string QueryToFileName(string value)
        {

            // query is treated case-sensitive so a case-insensitive encoding should be used
            // as the file system may be case-insensitive

            var bytes = Encoding.UTF8.GetBytes(value);
            var n = bytes.Length;

            var chars = new char[n << 1]; // * 2
            var j = 0;
            for (var i = 0; i < n; i++)
            {
                chars[j++] = hexChars[bytes[i] >> 4 & 0xF];
                chars[j++] = hexChars[bytes[i] & 0xF];
            }

            return new string(chars);
        }
    }
}
