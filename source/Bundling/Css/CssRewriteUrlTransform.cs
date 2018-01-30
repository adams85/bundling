using System;
using System.Text.RegularExpressions;
using Karambolo.AspNetCore.Bundling.Internal.Helpers;
using Microsoft.AspNetCore.Http;

namespace Karambolo.AspNetCore.Bundling.Css
{
    public class CssRewriteUrlTransform : BundleItemTransform
    {
        static string RemoveQuotes(ref string value)
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

        string RebaseUrl(string basePath, PathString pathPrefix, string value)
        {
            var quote = RemoveQuotes(ref value);

            if (quote == null || 
                value.StartsWith('/') ||
                value.StartsWith("data:", StringComparison.OrdinalIgnoreCase) || 
                !Uri.TryCreate(value, UriKind.RelativeOrAbsolute, out Uri uri) ||
                uri.IsAbsoluteUri)
                return value;

            var url = pathPrefix.Add(basePath + value);
            return string.Concat(quote, url, quote);
        }

        public override void Transform(IBundleItemTransformContext context)
        {
            if (context is IFileBundleItemTransformContext fileItemContext)
            {
                UrlUtils.GetFileName(fileItemContext.FilePath, out string basePath);

                var pathPrefix = context.BuildContext.HttpContext.Request.PathBase + context.BuildContext.BundlingContext.StaticFilesPathPrefix;

                context.Content = Regex.Replace(context.Content, @"(?<before>url\()(?<url>[^)]+?)(?<after>\))|(?<before>@import\s+)(?<url>['""][^'""]*['""])", 
                    m => string.Concat(m.Groups["before"].Value, RebaseUrl(basePath, pathPrefix, m.Groups["url"].Value), m.Groups["after"].Value));
            }
        }
    }
}
