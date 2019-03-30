using System;
using Microsoft.Extensions.DependencyInjection;

namespace Karambolo.AspNetCore.Bundling
{
    public sealed class LessBundlingModule : IBundlingModule
    {
        private readonly Action<BundleDefaultsOptions, IServiceProvider> _configureDefaults;

        public LessBundlingModule() : this(null) { }

        public LessBundlingModule(Action<BundleDefaultsOptions, IServiceProvider> configureDefaults)
        {
            _configureDefaults = configureDefaults;
        }

        public BundlingConfigurer Configure(BundlingConfigurer configurer)
        {
            return configurer.AddLess(_configureDefaults);
        }
    }
}
