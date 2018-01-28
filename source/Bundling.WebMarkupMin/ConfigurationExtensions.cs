using Karambolo.AspNetCore.Bundling.Css;
using Karambolo.AspNetCore.Bundling.Js;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Karambolo.AspNetCore.Bundling.WebMarkupMin
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
