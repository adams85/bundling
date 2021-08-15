using System;
using System.Threading.Tasks;
using Karambolo.AspNetCore.Bundling.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

        public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            if (Url != null)
            {
                IUrlHelper urlHelper = _urlHelperFactory.GetUrlHelper(ViewContext);

                if (ViewHelper.TryGetBundle(ViewContext.HttpContext, _bundleManagerFactory, urlHelper.Content(Url),
                    out QueryString query, out IBundleManager bundleManager, out IBundleModel bundle))
                {
                    return bundle.HtmlRenderer.RenderTagHelperAsync(context, output, urlHelper, bundleManager, bundle, query, UrlAttributeName);
                }
            }

            output.CopyHtmlAttribute(UrlAttributeName, context);
            return Task.CompletedTask;
        }
    }
}
