using System;
using System.Linq;
using System.Threading.Tasks;
using Karambolo.AspNetCore.Bundling.Css;
using Karambolo.AspNetCore.Bundling.Js;
using Karambolo.AspNetCore.Bundling.ViewHelpers;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Karambolo.AspNetCore.Bundling.Internal.Rendering
{
    public class SourceIncludesBundleHtmlRenderer : IBundleHtmlRenderer
    {
        public static readonly SourceIncludesBundleHtmlRenderer Instance = new SourceIncludesBundleHtmlRenderer();

        protected internal SourceIncludesBundleHtmlRenderer() { }

        private static string GetTagFormat(IBundleModel bundle)
        {
            if (CssBundleConfiguration.OutputMediaType.Equals(bundle.OutputMediaType, StringComparison.OrdinalIgnoreCase))
                return "<link href=\"{0}\" rel=\"stylesheet\"/>";

            if (JsBundleConfiguration.OutputMediaType.Equals(bundle.OutputMediaType, StringComparison.OrdinalIgnoreCase))
            {
                return
                    bundle.Transforms != null && bundle.Transforms.Any(t =>
                        t is IAggregatorBundleTransform &&
                        t.GetType() is Type type &&
                        type.FullName == "Karambolo.AspNetCore.Bundling.EcmaScript.ModuleBundlingTransform" &&
                        type.Assembly.GetName().Name == "Karambolo.AspNetCore.Bundling.EcmaScript") ?
                            "<script src=\"{0}\" type=\"module\"></script>" :
                            "<script src=\"{0}\"></script>";
            }

            return null;
        }

        public async Task<IHtmlContent> RenderHtmlAsync(IUrlHelper urlHelper, IBundleManager bundleManager, IBundleModel bundle,
            QueryString query, string tagFormat, bool? addVersion)
        {
            tagFormat = GetTagFormat(bundle);
            if (tagFormat == null)
                return HtmlString.Empty;

            HttpContext httpContext = urlHelper.ActionContext.HttpContext;

            Func<object, PathString, string, string> fileVersionAppender =
                ViewHelper.GetFileVersionAppender(httpContext, addVersion ?? true, out object fileVersionAppenderState);

            IBundleSourceBuildItem[] items = await bundleManager.GetBuildItemsAsync(urlHelper.ActionContext.HttpContext, bundle, query, loadItemContent: false);

            var builder = new HtmlContentBuilder(items.Length * 2 - 1);

            bool unresolvedUrlFound = false;
            bool isFirstAppend = true;

            for (int i = 0, n = items.Length; i < n; i++)
            {
                string url = bundle.SourceItemUrlResolver(items[i], bundleManager.BundlingContext, urlHelper);
                if (url == null)
                {
                    unresolvedUrlFound = true;
                    continue;
                }

                url = fileVersionAppender(fileVersionAppenderState, httpContext.Request.PathBase, url);

                if (isFirstAppend)
                    isFirstAppend = false;
                else
                    builder.AppendLine();

                builder.AppendHtml(new HtmlFormattableString(tagFormat, url));
            }

            if (unresolvedUrlFound)
            {
                ILogger logger = urlHelper.ActionContext.HttpContext.RequestServices.GetRequiredService<ILogger<SourceIncludesBundleHtmlRenderer>>();
                logger.LogWarning($"URL of one or more source items could not be resolved during rendering HTML includes for bundle '{{PATH}}'. You may set a custom URL resolver by the {nameof(BundlingServiceCollectionExtensions.UseSourceItemUrlResolver)} method of the configuration builders.", bundle.Path);
            }

            return builder;
        }

        public async Task RenderTagHelperAsync(TagHelperContext tagHelperContext, TagHelperOutput tagHelperOutput, IUrlHelper urlHelper, IBundleManager bundleManager, IBundleModel bundle,
            QueryString query, BundlingTagHelperBase tagHelper)
        {
            tagHelperOutput.SuppressOutput();
            tagHelperOutput.Content.SetHtmlContent(await RenderHtmlAsync(urlHelper, bundleManager, bundle, query, tagFormat: null, tagHelper.AddVersion));
        }
    }
}
