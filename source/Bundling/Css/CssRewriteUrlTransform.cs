﻿using System;
using System.Text.RegularExpressions;
using Karambolo.AspNetCore.Bundling.Internal.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Karambolo.AspNetCore.Bundling.Css
{
    public class CssRewriteUrlTransform : BundleItemTransform
    {
        private static readonly Regex s_rewriteUrlsRegex = new Regex(
            @"(?<before>url\()(?<url>'[^']+'|""[^""]+""|[^)]+)(?<after>\))|" +
            @"(?<before>@import\s+)(?<url>'[^']+'|""[^""]+"")(?<after>(?:\s[^;]+)?\s*;)",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Compiled);

        internal static string RebaseUrlCore(string value, string basePath, PathString virtualPathPrefix, PathString outputPath)
        {
            if (!UrlUtils.IsRelative(value))
                return value;

            value = UrlUtils.NormalizePath(virtualPathPrefix.Add(basePath).Add("/" + value), canonicalize: true);

            if (outputPath.HasValue)
                value = UrlUtils.MakeRelativePath(outputPath, value);

            return value;
        }

        protected virtual string RebaseUrl(string value, string basePath, PathString virtualPathPrefix, PathString outputPath)
        {
            return RebaseUrlCore(value, basePath, virtualPathPrefix, outputPath);
        }

        internal static string RewriteUrlsCore(string content, Func<string, Capture, string> rebaseUrl)
        {
            return s_rewriteUrlsRegex.Replace(content,
                m =>
                {
                    Group capture = m.Groups["url"];
                    var url = capture.Value;
                    var quote = StringUtils.RemoveQuotes(ref url);

                    return string.Concat(
                        m.Groups["before"].Value,
                        quote, rebaseUrl(url, capture), quote,
                        m.Groups["after"].Value);
                });
        }

        protected virtual string RewriteUrls(string content, string basePath, PathString virtualPathPrefix, PathString outputPath)
        {
            return RewriteUrlsCore(content, (url, _) => RebaseUrl(url, basePath, virtualPathPrefix, outputPath));
        }

        public override void Transform(IBundleItemTransformContext context)
        {
            if (context is IFileBundleItemTransformContext fileItemContext)
            {
                StringSegment filePathSegment = UrlUtils.NormalizePathSegment(fileItemContext.FilePath.Replace('\\', '/'));
                UrlUtils.GetFileNameSegment(filePathSegment, out StringSegment basePathSegment);
                basePathSegment = UrlUtils.NormalizePathSegment(basePathSegment, trailingNormalization: PathNormalization.ExcludeSlash);

                var virtualPathPrefix = UrlUtils.NormalizePath(context.BuildContext.BundlingContext.StaticFilesPathPrefix, trailingNormalization: PathNormalization.ExcludeSlash);

                PathString outputPath = context.BuildContext.BundlingContext.BundlesPathPrefix.Add(context.BuildContext.Bundle.Path);

                context.Content = RewriteUrls(context.Content, basePathSegment.Value, virtualPathPrefix, outputPath);
            }
        }
    }
}
