using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

namespace Karambolo.AspNetCore.Bundling.Internal.Helpers
{
    internal enum PathNormalization
    {
        None,
        ExcludeSlash,
        IncludeSlash,
    }

    internal static class UrlUtils
    {
        private const string HexChars = "0123456789abcdef";

        private static readonly char[] s_illegalFileNameChars = Path.GetInvalidPathChars().Concat(Path.GetInvalidFileNameChars()).ToArray();
        private static readonly Uri s_dummyBaseUri = new Uri("http://host");

        private static readonly Lazy<Func<string, string>> s_getCanonicalPath = new Lazy<Func<string, string>>(() =>
        {
            var uriUtilsType = Type.GetType("Karambolo.Common.UriUtils, Karambolo.Common", throwOnError: false, ignoreCase: false);
            System.Reflection.MethodInfo getCanonicalNameMethod = uriUtilsType?.GetMethod("GetCanonicalPath", new[] { typeof(string) });
            return getCanonicalNameMethod != null ?
                (Func<string, string>)Delegate.CreateDelegate(typeof(Func<string, string>), getCanonicalNameMethod) :
                path => new UriBuilder { Path = path }.Uri.LocalPath; // fallback
        }, LazyThreadSafetyMode.PublicationOnly);

        public static void FromRelative(string url, out PathString path, out QueryString query, out FragmentString fragment)
        {
            var uri = new Uri(s_dummyBaseUri, url);
            UriHelper.FromAbsolute(uri.ToString(), out string scheme, out HostString host, out path, out query, out fragment);
        }

        public static QueryString NormalizeQuery(QueryString query, out IDictionary<string, StringValues> parsedQuery)
        {
            if (!query.HasValue)
            {
                parsedQuery = null;
                return query;
            }

            Dictionary<string, StringValues> parsed = QueryHelpers.ParseQuery(query.ToString());

            var builder = new QueryBuilder();
            foreach (KeyValuePair<string, StringValues> kvp in parsed.OrderBy(kvp => kvp.Key))
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

        public static string NormalizePath(string path,
            PathNormalization leadingNormalization = PathNormalization.IncludeSlash,
            PathNormalization trailingNormalization = PathNormalization.None,
            bool canonicalize = false)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            if (canonicalize)
                path = s_getCanonicalPath.Value(path);

            switch (leadingNormalization)
            {
                case PathNormalization.IncludeSlash:
                    path = path.StartsWith("/") ? path : '/' + path;
                    break;
                case PathNormalization.ExcludeSlash:
                    path = path.StartsWith("/") ? path.Substring(1) : path;
                    break;
            }

            switch (trailingNormalization)
            {
                case PathNormalization.IncludeSlash:
                    path = path.EndsWith("/") ? path : path + '/';
                    break;
                case PathNormalization.ExcludeSlash:
                    path = path.EndsWith("/") ? path.Substring(0, path.Length - 1) : path;
                    break;
            }

            return path;
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
                chars[i] = Array.IndexOf(s_illegalFileNameChars, c = chars[i]) < 0 ? char.ToLowerInvariant(c) : '_';

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
                chars[j++] = HexChars[bytes[i] >> 4 & 0xF];
                chars[j++] = HexChars[bytes[i] & 0xF];
            }

            return new string(chars);
        }
    }
}
