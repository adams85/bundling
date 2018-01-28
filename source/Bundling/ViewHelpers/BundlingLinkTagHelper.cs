using Microsoft.AspNetCore.Razor.TagHelpers;
using Karambolo.AspNetCore.Bundling.Internal;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Karambolo.AspNetCore.Bundling.ViewHelpers
{
    [HtmlTargetElement("link", Attributes = urlAttributeName + ", [rel=stylesheet]", TagStructure = TagStructure.WithoutEndTag)]
    public class BundlingLinkTagHelper : BundlingTagHelperBase
    {
        const string urlAttributeName = "href";

        public BundlingLinkTagHelper(IBundleManagerFactory bundleManagerFactory, IUrlHelperFactory urlHelperFactory)
            : base(bundleManagerFactory, urlHelperFactory) { }

        [HtmlAttributeName(urlAttributeName)]
        public string Href { get; set; }

        protected override string UrlAttributeName => urlAttributeName;
        protected override string Url => Href;
    }
}