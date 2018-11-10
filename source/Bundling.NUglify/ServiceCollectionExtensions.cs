using System;
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
        public static BundlingConfigurer UseNUglify(this BundlingConfigurer configurer, CssSettings cssSettings = null, CodeSettings jsSettings = null)
        {
            if (configurer == null)
                throw new ArgumentNullException(nameof(configurer));

            configurer.Services.Replace(ServiceDescriptor.Singleton<ICssMinifier>(sp => new CssMinifier(cssSettings, sp.GetRequiredService<ILoggerFactory>())));
            configurer.Services.Replace(ServiceDescriptor.Singleton<IJsMinifier>(sp => new JsMinifier(jsSettings, sp.GetRequiredService<ILoggerFactory>())));

            return configurer;
        }
    }
}
