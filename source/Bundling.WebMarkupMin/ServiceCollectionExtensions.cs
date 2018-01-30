using Karambolo.AspNetCore.Bundling.Css;
using Karambolo.AspNetCore.Bundling.Js;
using Karambolo.AspNetCore.Bundling.WebMarkupMin;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ConfigurationExtensions
    {
        public static BundlingConfigurer UseWebMarkupMin(this BundlingConfigurer @this)
        {
            @this.Services.Replace(ServiceDescriptor.Singleton<ICssMinifier, CssMinifier>());
            @this.Services.Replace(ServiceDescriptor.Singleton<IJsMinifier, JsMinifier>());

            return @this;
        }
    }
}
