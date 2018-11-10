using System;
using Karambolo.AspNetCore.Bundling;
using Karambolo.AspNetCore.Bundling.Less;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ConfigurationExtensions
    {
        public static BundlingConfigurer AddLess(this BundlingConfigurer configurer, Action<BundleDefaultsOptions, IServiceProvider> configureDefaults = null)
        {
            if (configurer == null)
                throw new ArgumentNullException(nameof(configurer));

            configurer.Services.AddSingleton<IConfigureOptions<BundleDefaultsOptions>>(sp => new LessBundleConfiguration.Configurer(configureDefaults, sp));
            configurer.Services.AddSingleton<IConfigurationHelper, LessBundleConfiguration.Helper>();
            configurer.Services.AddSingleton<IExtensionMapper, LessBundleConfiguration.ExtensionMapper>();

            configurer.Services.AddSingleton<ILessEngineFactory, LessEngineFactory>();
            configurer.Services.AddSingleton<ILessCompiler, LessCompiler>();

            return configurer;
        }
    }
}
