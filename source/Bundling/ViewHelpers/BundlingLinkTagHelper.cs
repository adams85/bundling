using Karambolo.AspNetCore.Bundling.Internal;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Karambolo.AspNetCore.Bundling.ViewHelpers
{
    [HtmlTargetElement("link", Attributes = UrlAttributeNameConst + ", [rel=stylesheet]", TagStructure = TagStructure.WithoutEndTag)]
    [HtmlTargetElement("link", Attributes = AddVersionAttributeName + ", [rel=stylesheet]", TagStructure = TagStructure.WithoutEndTag)]
    public class BundlingLinkTagHelper : BundlingTagHelperBase
    {
        private const string UrlAttributeNameConst = "href";

        public BundlingLinkTagHelper(IBundleManagerFactory bundleManagerFactory, IUrlHelperFactory urlHelperFactory)
            : base(bundleManagerFactory, urlHelperFactory) { }

        [HtmlAttributeName(UrlAttributeNameConst)]
        public string Href { get; set; }

        protected internal override string UrlAttributeName => UrlAttributeNameConst;
        protected internal override string Url => Href;
    }
}
