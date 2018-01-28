using Microsoft.AspNetCore.Razor.TagHelpers;
using Karambolo.AspNetCore.Bundling.Internal;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Karambolo.AspNetCore.Bundling.ViewHelpers
{
    [HtmlTargetElement("script", Attributes = urlAttributeName)]
    public class BundlingScriptTagHelper : BundlingTagHelperBase
    {
        const string urlAttributeName = "src";

        public BundlingScriptTagHelper(IBundleManagerFactory bundleManagerFactory, IUrlHelperFactory urlHelperFactory)
            : base(bundleManagerFactory, urlHelperFactory) { }

        [HtmlAttributeName(urlAttributeName)]
        public string Src { get; set; }

        protected override string UrlAttributeName => urlAttributeName;
        protected override string Url => Src;
    }
}