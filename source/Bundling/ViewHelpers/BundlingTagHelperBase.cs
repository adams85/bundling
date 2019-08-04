using System;
using System.Threading.Tasks;
using Karambolo.AspNetCore.Bundling.Internal;
using Karambolo.AspNetCore.Bundling.Internal.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Karambolo.AspNetCore.Bundling.ViewHelpers
{
    public abstract class BundlingTagHelperBase : TagHelper
    {
        private readonly IBundleManagerFactory _bundleManagerFactory;
        private readonly IUrlHelperFactory _urlHelperFactory;

        public BundlingTagHelperBase(IBundleManagerFactory bundleManagerFactory, IUrlHelperFactory urlHelperFactory)
        {
            if (bundleManagerFactory == null)
                throw new ArgumentNullException(nameof(bundleManagerFactory));

            if (urlHelperFactory == null)
                throw new ArgumentNullException(nameof(urlHelperFactory));

            _bundleManagerFactory = bundleManagerFactory;

            _urlHelperFactory = urlHelperFactory;
        }

        public override int Order => -10000;

        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; }

        protected abstract string UrlAttributeName { get; }
        protected abstract string Url { get; }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            if (Url == null)
                return;

            output.CopyHtmlAttribute(UrlAttributeName, context);

            Microsoft.AspNetCore.Mvc.IUrlHelper urlHelper = _urlHelperFactory.GetUrlHelper(ViewContext);
            UrlUtils.FromRelative(urlHelper.Content(Url), out PathString path, out QueryString query, out _);

            string url = null;
            var n = _bundleManagerFactory.Instances.Count;
            for (var i = 0; i < n; i++)
                if ((url = await _bundleManagerFactory.Instances[i].TryGenerateUrlAsync(path, query, ViewContext.HttpContext)) != null)
                    break;

            if (url != null)
            {
                var index = output.Attributes.IndexOfName(UrlAttributeName);
                TagHelperAttribute existingAttribute = output.Attributes[index];
                output.Attributes[index] = new TagHelperAttribute(existingAttribute.Name, url, existingAttribute.ValueStyle);
            }
        }
    }
}
