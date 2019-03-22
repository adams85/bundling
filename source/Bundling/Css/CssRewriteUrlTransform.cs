using System;
using System.Text.RegularExpressions;
using Karambolo.AspNetCore.Bundling.Internal.Helpers;
using Microsoft.AspNetCore.Http;

namespace Karambolo.AspNetCore.Bundling.Css
{
    public class CssRewriteUrlTransform : BundleItemTransform
    {
        private static readonly Regex s_rewriteUrlsRegex = new Regex(
            @"(?<before>url\()(?<url>'[^']+'|""[^""]+""|[^)]+)(?<after>\))|" +
            @"(?<before>@import\s+)(?<url>'[^']+'|""[^""]+"")(?<after>\s*;)",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        protected virtual string RebaseUrl(string value, string basePath, string pathPrefix)
        {
            if (value.StartsWith('/') ||
                value.StartsWith("data:", StringComparison.OrdinalIgnoreCase) ||
                !Uri.TryCreate(value, UriKind.RelativeOrAbsolute, out Uri uri) ||
                uri.IsAbsoluteUri)
                return value;

            return new PathString(pathPrefix).Add(basePath + value);
        }

        protected virtual string RewriteUrls(string content, string basePath, string pathPrefix)
        {
            return s_rewriteUrlsRegex.Replace(content,
                m =>
                {
                    var value = m.Groups["url"].Value;
                    var quote = StringUtils.RemoveQuotes(ref value);

                    return string.Concat(
                        m.Groups["before"].Value,
                        quote, RebaseUrl(value, basePath, pathPrefix), quote,
                        m.Groups["after"].Value);
                });
        }

        public override void Transform(IBundleItemTransformContext context)
        {
            if (context is IFileBundleItemTransformContext fileItemContext)
            {
                UrlUtils.GetFileName(fileItemContext.FilePath, out string basePath);

                PathString pathPrefix = context.BuildContext.HttpContext.Request.PathBase + context.BuildContext.BundlingContext.StaticFilesPathPrefix;

                context.Content = RewriteUrls(context.Content, basePath, pathPrefix);
            }
        }
    }
}
