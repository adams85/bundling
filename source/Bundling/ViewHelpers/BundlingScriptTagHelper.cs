﻿using Karambolo.AspNetCore.Bundling.Internal;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Karambolo.AspNetCore.Bundling.ViewHelpers
{
    [HtmlTargetElement("script", Attributes = UrlAttributeNameConst)]
    [HtmlTargetElement("script", Attributes = AddVersionAttributeName)]
    public class BundlingScriptTagHelper : BundlingTagHelperBase
    {
        private const string UrlAttributeNameConst = "src";

        public BundlingScriptTagHelper(IBundleManagerFactory bundleManagerFactory, IUrlHelperFactory urlHelperFactory)
            : base(bundleManagerFactory, urlHelperFactory) { }

        [HtmlAttributeName(UrlAttributeNameConst)]
        public string Src { get; set; }

        protected internal override string UrlAttributeName => UrlAttributeNameConst;
        protected internal override string Url => Src;
    }
}
