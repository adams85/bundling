using Karambolo.AspNetCore.Bundling.Css;
using Karambolo.AspNetCore.Bundling.Js;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using NUglify.Css;
using NUglify.JavaScript;

namespace Karambolo.AspNetCore.Bundling.NUglify
{
    public static class ConfigurationExtensions
    {
        public static BundlingConfigurer UseNUglify(this BundlingConfigurer @this, CssSettings cssSettings = null, CodeSettings jsSettings = null)
        {
            @this.Services.Replace(ServiceDescriptor.Singleton<ICssMinifier>(sp => new CssMinifier(cssSettings, sp.GetRequiredService<ILoggerFactory>())));
            @this.Services.Replace(ServiceDescriptor.Singleton<IJsMinifier>(sp => new JsMinifier(jsSettings, sp.GetRequiredService<ILoggerFactory>())));

            return @this;
        }
    }
}
