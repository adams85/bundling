using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

        private static readonly char[] s_illegalFileNameChars = new HashSet<char>(Path.GetInvalidPathChars().Concat(Path.GetInvalidFileNameChars())).ToArray();

        // https://stackoverflow.com/questions/3641722/valid-characters-for-uri-schemes
        private static readonly Regex s_hasSchemeRegex = new Regex(@"^[a-zA-Z][a-zA-Z0-9+\-.]*:", RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static readonly Uri s_dummyBaseUri = new Uri("xx://");

        private static Func<string, string> s_getCanonicalPath;

        private static string GetCanonicalPath(string path)
        {
            return LazyInitializer.EnsureInitialized(ref s_getCanonicalPath, () =>
            {
                var uriUtilsType = Type.GetType("Karambolo.Common.UriUtils, Karambolo.Common", throwOnError: false, ignoreCase: false);
                System.Reflection.MethodInfo getCanonicalNameMethod = uriUtilsType?.GetMethod("GetCanonicalPath", new[] { typeof(string) });

                if (getCanonicalNameMethod != null)
                    return (Func<string, string>)Delegate.CreateDelegate(typeof(Func<string, string>), getCanonicalNameMethod);

                // fallback
                var collapseSlashesRegex = new Regex(@"//*", RegexOptions.CultureInvariant | RegexOptions.Compiled);
                return value => new UriBuilder { Path = collapseSlashesRegex.Replace(value, "/") }.Uri.LocalPath;
            })(path);
        }

        public static bool IsRelative(string url)
        {
            return !url.StartsWith("/") && !s_hasSchemeRegex.IsMatch(url);
        }

        public static void FromRelative(string url, out PathString path, out QueryString query, out FragmentString fragment)
        {
            var uri = new Uri(s_dummyBaseUri, url);
            UriHelper.FromAbsolute(uri.ToString(), out _, out _, out path, out query, out fragment);
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
            return NormalizePathSegment(path, leadingNormalization, trailingNormalization, canonicalize).Value;
        }

        public static StringSegment NormalizePathSegment(StringSegment path,
            PathNormalization leadingNormalization = PathNormalization.IncludeSlash,
            PathNormalization trailingNormalization = PathNormalization.None,
            bool canonicalize = false)
        {
            if (!path.HasValue)
                path = StringSegment.Empty;

            if (canonicalize)
                path = GetCanonicalPath(path.Value);

            int pathLength = path.Length;
            switch (leadingNormalization)
            {
                case PathNormalization.IncludeSlash:
                    if (pathLength == 0 || path[0] != '/')
                    {
                        path = "/" + path;
                        pathLength++;
                    }
                    break;
                case PathNormalization.ExcludeSlash:
                    if (pathLength > 0 && path[0] == '/' && (pathLength > 1 || trailingNormalization != PathNormalization.IncludeSlash))
                    {
                        path = path.Subsegment(1);
                        pathLength--;
                    }
                    break;
            }

            switch (trailingNormalization)
            {
                case PathNormalization.IncludeSlash:
                    if (pathLength == 0 || path[path.Length - 1] != '/')
                        path = path + "/";
                    break;
                case PathNormalization.ExcludeSlash:
                    if (pathLength > 0 && path[path.Length - 1] == '/' && (pathLength > 1 || leadingNormalization != PathNormalization.IncludeSlash))
                        path = path.Subsegment(0, pathLength - 1);
                    break;
            }

            return path;
        }

        public static string MakeRelativePath(string basePath, string path)
        {
            return new Uri(s_dummyBaseUri, basePath).MakeRelativeUri(new Uri(s_dummyBaseUri, path)).ToString();
        }

        public static string GetFileName(string path, out string basePath)
        {
            string fileName = GetFileNameSegment(path, out StringSegment basePathSegment).Value;
            basePath = basePathSegment.Value;
            return fileName;
        }

        public static StringSegment GetFileNameSegment(StringSegment path, out StringSegment basePath)
        {
            if (path.Length == 0)
                return basePath = StringSegment.Empty;

            var index = path.LastIndexOf('/') + 1;

            basePath = path.Subsegment(0, index);
            return path.Subsegment(index);
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
