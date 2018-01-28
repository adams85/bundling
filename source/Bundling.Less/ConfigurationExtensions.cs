using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Karambolo.AspNetCore.Bundling.Less
{
    public static class ConfigurationExtensions
    {
        public static BundlingConfigurer AddLess(this BundlingConfigurer @this, Action<BundleDefaultsOptions, IServiceProvider> configureDefaults = null)
        {
            @this.Services.AddSingleton<IConfigureOptions<BundleDefaultsOptions>>(sp => new LessBundleConfiguration.Configurer(configureDefaults, sp));
            @this.Services.AddSingleton<IConfigurationHelper, LessBundleConfiguration.Helper>();
            @this.Services.AddSingleton<IExtensionMapper, LessBundleConfiguration.ExtensionMapper>();

            @this.Services.AddSingleton<ILessEngineFactory, LessEngineFactory>();
            @this.Services.AddSingleton<ILessCompiler, LessCompiler>();

            return @this;
        }

        public static BundleConfigurer AddLess(this BundleCollectionConfigurer @this, PathString path)
        {
            var bundle = new Bundle(path, @this.GetDefaults(LessBundleConfiguration.BundleType));
            @this.Bundles.Add(bundle);
            return new BundleConfigurer(bundle, @this.Bundles.SourceFileProvider, @this.AppServices);
        }
    }
}
