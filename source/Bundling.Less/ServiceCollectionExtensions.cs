using System;
using Karambolo.AspNetCore.Bundling;
using Karambolo.AspNetCore.Bundling.Less;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
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
    }
}
