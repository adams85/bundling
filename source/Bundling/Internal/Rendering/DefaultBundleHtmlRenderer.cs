using System.Threading.Tasks;
using Karambolo.AspNetCore.Bundling.ViewHelpers;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Karambolo.AspNetCore.Bundling.Internal.Rendering
{
    public class DefaultBundleHtmlRenderer : IBundleHtmlRenderer
    {
        public static readonly DefaultBundleHtmlRenderer Instance = new DefaultBundleHtmlRenderer();

        protected internal DefaultBundleHtmlRenderer() { }

        public async Task<IHtmlContent> RenderHtmlAsync(IUrlHelper urlHelper, IBundleManager bundleManager, IBundleModel bundle,
            QueryString query, string tagFormat, bool? addVersion)
        {
            string url = await bundleManager.GenerateUrlAsync(urlHelper.ActionContext.HttpContext, bundle, query, addVersion ?? true);

            return new HtmlString(string.Format(tagFormat, url));
        }

        public async Task RenderTagHelperAsync(TagHelperContext tagHelperContext, TagHelperOutput tagHelperOutput, IUrlHelper urlHelper, IBundleManager bundleManager, IBundleModel bundle,
            QueryString query, BundlingTagHelperBase tagHelper)
        {
            string url = await bundleManager.GenerateUrlAsync(urlHelper.ActionContext.HttpContext, bundle, query, tagHelper.AddVersion ?? true);

            TagHelperAttribute existingAttribute = tagHelperContext.AllAttributes[tagHelper.UrlAttributeName];
            tagHelperOutput.Attributes.SetAttribute(new TagHelperAttribute(existingAttribute.Name, url, existingAttribute.ValueStyle));
        }
    }
}
