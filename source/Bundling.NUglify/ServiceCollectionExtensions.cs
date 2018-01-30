using Karambolo.AspNetCore.Bundling.Css;
using Karambolo.AspNetCore.Bundling.Js;
using Karambolo.AspNetCore.Bundling.NUglify;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using NUglify.Css;
using NUglify.JavaScript;

namespace Microsoft.Extensions.DependencyInjection
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
