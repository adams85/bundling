using System.Threading.Tasks;
using Karambolo.AspNetCore.Bundling.ViewHelpers;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Karambolo.AspNetCore.Bundling.Internal.Rendering
{
    public class DefaultBundleHtmlRenderer : IBundleHtmlRenderer
    {
        public static readonly DefaultBundleHtmlRenderer Instance = new DefaultBundleHtmlRenderer();

        protected internal DefaultBundleHtmlRenderer() { }

        public async Task<IHtmlContent> RenderHtmlAsync(IUrlHelper urlHelper, IBundleManager bundleManager, IBundleModel bundle,
            QueryString query, string tagFormat, bool addVersion)
        {
            string url = await bundleManager.GenerateUrlAsync(urlHelper.ActionContext.HttpContext, bundle, query, addVersion);

            return new HtmlFormattableString(tagFormat, url);
        }

        public async Task RenderTagHelperAsync(TagHelperContext tagHelperContext, TagHelperOutput tagHelperOutput, IUrlHelper urlHelper, IBundleManager bundleManager, IBundleModel bundle,
            QueryString query, BundlingTagHelperBase tagHelper)
        {
            string url = await bundleManager.GenerateUrlAsync(urlHelper.ActionContext.HttpContext, bundle, query, tagHelper.ActualAddVersion);

            tagHelperOutput.CopyHtmlAttribute(tagHelper.UrlAttributeName, tagHelperContext);

            TagHelperAttribute existingAttribute = tagHelperContext.AllAttributes[tagHelper.UrlAttributeName];
            var index = tagHelperOutput.Attributes.IndexOfName(tagHelper.UrlAttributeName);
            tagHelperOutput.Attributes[index] = new TagHelperAttribute(existingAttribute.Name, url, existingAttribute.ValueStyle);
        }
    }
}
