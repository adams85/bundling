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

#if NETCOREAPP3_0_OR_GREATER
        internal const string AddVersionAttributeName = "bundling-add-version";

        [HtmlAttributeName(AddVersionAttributeName)]
        public bool? AddVersion { get; set; }
#else
        internal bool? AddVersion => null;
#endif

        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; }

        protected internal abstract string UrlAttributeName { get; }
        protected internal abstract string Url { get; }

        public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            if (Url != null)
            {
                IUrlHelper urlHelper = _urlHelperFactory.GetUrlHelper(ViewContext);
                string url = urlHelper.Content(Url);

                if (ViewHelper.TryGetBundle(ViewContext.HttpContext, _bundleManagerFactory, url,
                    out QueryString query, out IBundleManager bundleManager, out IBundleModel bundle))
                {
                    return bundle.HtmlRenderer.RenderTagHelperAsync(context, output, urlHelper, bundleManager, bundle, query, this);
                }

                if (AddVersion ?? true)
                {
                    Func<object, PathString, string, string> fileVersionAppender =
                        ViewHelper.GetFileVersionAppender(ViewContext.HttpContext, addVersion: true, out object fileVersionAppenderState);

                    url = fileVersionAppender(fileVersionAppenderState, ViewContext.HttpContext.Request.PathBase, url);

                    TagHelperAttribute existingAttribute = context.AllAttributes[UrlAttributeName];
                    output.Attributes.SetAttribute(new TagHelperAttribute(existingAttribute.Name, url, existingAttribute.ValueStyle));
                }
                else
                    output.CopyHtmlAttribute(UrlAttributeName, context);
            }

            return Task.CompletedTask;
        }
    }
}
