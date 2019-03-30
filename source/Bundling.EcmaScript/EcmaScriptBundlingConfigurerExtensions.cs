using System;
using Karambolo.AspNetCore.Bundling.EcmaScript;
using Karambolo.AspNetCore.Bundling.EcmaScript.Internal;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class EcmaScriptBundlingConfigurerExtensions
    {
        public static BundlingConfigurer AddEcmaScript(this BundlingConfigurer configurer)
        {
            if (configurer == null)
                throw new ArgumentNullException(nameof(configurer));

            configurer.Services.AddSingleton<IModuleBundlerFactory, DefaultModuleBundlerFactory>();

            return configurer;
        }
    }
}
