using System;
using Karambolo.AspNetCore.Bundling.Css;
using Karambolo.AspNetCore.Bundling.Js;
using Karambolo.AspNetCore.Bundling.WebMarkupMin;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ConfigurationExtensions
    {
        public static BundlingConfigurer UseWebMarkupMin(this BundlingConfigurer configurer)
        {
            if (configurer == null)
                throw new ArgumentNullException(nameof(configurer));

            configurer.Services.Replace(ServiceDescriptor.Singleton<ICssMinifier, CssMinifier>());
            configurer.Services.Replace(ServiceDescriptor.Singleton<IJsMinifier, JsMinifier>());

            return configurer;
        }
    }
}
