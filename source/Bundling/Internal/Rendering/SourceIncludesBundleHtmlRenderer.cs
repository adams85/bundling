using System;
using System.Linq;
using System.Runtime.CompilerServices;
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

        private readonly ConditionalWeakTable<IBundleModel, object> _hasLoggedWarningFlags;

        protected SourceIncludesBundleHtmlRenderer()
        {
            _hasLoggedWarningFlags = new ConditionalWeakTable<IBundleModel, object>();
        }

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
            QueryString query, string tagFormat, bool addVersion, StaticFileUrlToFileMapper urlToFileMapper)
        {
            tagFormat = GetTagFormat(bundle);
            if (tagFormat == null)
                return HtmlString.Empty;

            HttpContext httpContext = urlHelper.ActionContext.HttpContext;

            IStaticFileUrlHelper staticFileUrlHelper = addVersion ? httpContext.RequestServices.GetRequiredService<IStaticFileUrlHelper>() : null;

            IBundleSourceBuildItem[] items = await bundleManager.GetBuildItemsAsync(httpContext, bundle, query, loadItemContent: false);
            if (items.Length == 0)
                return HtmlString.Empty;

            var builder = new HtmlContentBuilder(items.Length * 2 - 1);

            bool nonMappableItemFound = false;
            bool isFirstAppend = true;

            for (int i = 0, n = items.Length; i < n; i++)
            {
                IBundleSourceBuildItem item = items[i];

                string url = bundle.SourceItemToUrlMapper(item, bundleManager.BundlingContext, urlHelper);
                if (url == null)
                {
                    nonMappableItemFound = true;
                    continue;
                }

                if (staticFileUrlHelper != null)
                {
                    url =
                        item.ItemTransformContext is IFileBundleItemTransformContext fileItemContext ?
                        staticFileUrlHelper.AddVersion(url, urlHelper, fileItemContext.FileProvider, fileItemContext.FilePath) :
                        staticFileUrlHelper.AddVersion(url, urlHelper, urlToFileMapper);
                }

                if (isFirstAppend)
                    isFirstAppend = false;
                else
                    builder.AppendLine();

                builder.AppendHtml(new HtmlFormattableString(tagFormat, url));
            }

            if (nonMappableItemFound)
            {
                ILogger logger = httpContext.RequestServices.GetRequiredService<ILogger<SourceIncludesBundleHtmlRenderer>>();

                _hasLoggedWarningFlags.GetValue(bundle, b =>
                {
                    logger.LogWarning($"One or more source items could not be mapped to an URL during rendering HTML includes for bundle '{{PATH}}'. You may set a custom mapper by the {nameof(BundlingServiceCollectionExtensions.UseSourceItemToUrlMapper)} method of the configuration builders.", b.Path);
                    return new object();
                });
            }

            return builder;
        }

        public async Task RenderTagHelperAsync(TagHelperContext tagHelperContext, TagHelperOutput tagHelperOutput, IUrlHelper urlHelper, IBundleManager bundleManager, IBundleModel bundle,
            QueryString query, BundlingTagHelperBase tagHelper)
        {
            tagHelperOutput.SuppressOutput();
            tagHelperOutput.Content.SetHtmlContent(await RenderHtmlAsync(urlHelper, bundleManager, bundle, query, tagFormat: null, tagHelper.ActualAddVersion, tagHelper.UrlToFileMapper));
        }
    }
}
