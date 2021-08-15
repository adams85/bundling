﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Karambolo.AspNetCore.Bundling.Internal
{
    public interface IBundleHtmlRenderer
    {
        Task<IHtmlContent> RenderHtmlAsync(IUrlHelper urlHelper, IBundleManager bundleManager, IBundleModel bundle,
            QueryString query, string tagFormat);

        Task RenderTagHelperAsync(TagHelperContext tagHelperContext, TagHelperOutput tagHelperOutput, IUrlHelper urlHelper, IBundleManager bundleManager, IBundleModel bundle,
            QueryString query, string urlAttributeName);
    }
}
