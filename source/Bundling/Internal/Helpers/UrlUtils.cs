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
    internal enum UrlKind
    {
        Invalid,
        Absolute,
        RelativeWithAuthority,
        RelativeAndAbsolutePath,
        RelativeAndRelativePath
    }

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

        private static readonly Uri s_dummyBaseUri = new Uri("xx:");

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

        // https://stackoverflow.com/questions/904046/absolute-urls-relative-urls-and#answer-904066
        public static UrlKind ClassifyUrl(string url)
        {
            if (url.StartsWith("/", StringComparison.Ordinal))
                return url.Length > 1 && url[1] == '/' ? UrlKind.RelativeWithAuthority : UrlKind.RelativeAndAbsolutePath;

            if (!s_hasSchemeRegex.IsMatch(url))
                return !string.IsNullOrWhiteSpace(url) ? UrlKind.RelativeAndRelativePath : UrlKind.Invalid;

            return UrlKind.Absolute;
        }

        public static bool IsRelativePath(string url)
        {
            return ClassifyUrl(url) == UrlKind.RelativeAndRelativePath;
        }

        public static void DeconstructPath(string url, out PathString path, out QueryString query, out FragmentString fragment)
        {
            char c;
            var index = 0;

            for (; index < url.Length; index++)
                if ((c = url[index]) == '#')
                {
                    path = PathString.FromUriComponent(NormalizePathSegment(new StringSegment(url, 0, index)).Value);
                    query = default;
                    fragment = FragmentString.FromUriComponent(url.Substring(index));
                    return;
                }
                else if (c == '?')
                {
                    path = PathString.FromUriComponent(NormalizePathSegment(new StringSegment(url, 0, index)).Value);
                    goto hasQuery;
                }

            path = PathString.FromUriComponent(NormalizePathSegment(url).Value);
            query = default;
            fragment = default;
            return;

hasQuery:
            var queryIndex = index++;
            for (; index < url.Length; index++)
                if (url[index] == '#')
                {
                    query = QueryString.FromUriComponent(url.Substring(queryIndex, index - queryIndex));
                    fragment = FragmentString.FromUriComponent(url.Substring(index));
                    return;
                }

            query = QueryString.FromUriComponent(url.Substring(queryIndex));
            fragment = default;
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
            foreach ((string key, StringValues values) in parsed.OrderBy(kvp => kvp.Key))
                for (int i = 0, n = values.Count; i < n; i++)
                    builder.Add(key, values[i]);

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

        public static string NormalizeDirectorySeparators(string path)
        {
            return path.Replace('\\', '/');
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
#if !NETCOREAPP3_0_OR_GREATER
            var chars = new char[value.Length];
            var source = value;
#else
            return string.Create(value.Length, value, (chars, source) =>
            {
#endif
                char c;
                for (int i = 0; i < source.Length; i++)
                    chars[i] = Array.IndexOf(s_illegalFileNameChars, c = source[i]) < 0 ? char.ToLowerInvariant(c) : '_';
#if NETCOREAPP3_0_OR_GREATER
            });
#else
            return new string(chars);
#endif
        }

        public static string QueryToFileName(string value)
        {
            // query is treated case-sensitive so a case-insensitive encoding should be used
            // as the file system may be case-insensitive

            var bytes = Encoding.UTF8.GetBytes(value);

#if !NETCOREAPP3_0_OR_GREATER
            var chars = new char[bytes.Length * 2];
            var source = bytes;
#else
            return string.Create(bytes.Length * 2, bytes, (chars, source) =>
            {
#endif
                var j = 0;
                for (var i = 0; i < source.Length; i++)
                {
                    chars[j++] = HexChars[source[i] >> 4 & 0xF];
                    chars[j++] = HexChars[source[i] & 0xF];
                }
#if NETCOREAPP3_0_OR_GREATER
            });
#else
            return new string(chars);
#endif
        }
    }
}
