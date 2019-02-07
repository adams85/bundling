using System;
using Karambolo.AspNetCore.Bundling;
using Karambolo.AspNetCore.Bundling.Sass;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ConfigurationExtensions
    {
        public static BundlingConfigurer AddSass(this BundlingConfigurer configurer, Action<BundleDefaultsOptions, IServiceProvider> configureDefaults = null)
        {
            if (configurer == null)
                throw new ArgumentNullException(nameof(configurer));

            configurer.Services.AddSingleton<IConfigureOptions<BundleDefaultsOptions>>(sp => new SassBundleConfiguration.Configurer(configureDefaults, sp));
            configurer.Services.AddSingleton<IConfigurationHelper, SassBundleConfiguration.Helper>();
            configurer.Services.AddSingleton<IExtensionMapper, SassBundleConfiguration.ExtensionMapper>();

            configurer.Services.AddSingleton<ISassCompiler, SassCompiler>();

            LibSassHost.SassCompiler.FileManager = FileProviderFileManager.Instance;

            return configurer;
        }
    }
}
