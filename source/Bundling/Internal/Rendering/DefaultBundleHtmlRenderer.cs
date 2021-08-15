using System.Threading.Tasks;
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
            QueryString query, string tagFormat)
        {
            string url = await bundleManager.GenerateUrlAsync(urlHelper.ActionContext.HttpContext, bundle, query);

            return new HtmlString(string.Format(tagFormat, url));
        }

        public async Task RenderTagHelperAsync(TagHelperContext tagHelperContext, TagHelperOutput tagHelperOutput, IUrlHelper urlHelper, IBundleManager bundleManager, IBundleModel bundle,
            QueryString query, string urlAttributeName)
        {
            string url = await bundleManager.GenerateUrlAsync(urlHelper.ActionContext.HttpContext, bundle, query);

            tagHelperOutput.CopyHtmlAttribute(urlAttributeName, tagHelperContext);

            var index = tagHelperOutput.Attributes.IndexOfName(urlAttributeName);
            TagHelperAttribute existingAttribute = tagHelperOutput.Attributes[index];
            tagHelperOutput.Attributes[index] = new TagHelperAttribute(existingAttribute.Name, url, existingAttribute.ValueStyle);
        }
    }
}
