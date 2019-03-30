using System;
using Microsoft.Extensions.DependencyInjection;

namespace Karambolo.AspNetCore.Bundling
{
    public sealed class SassBundlingModule : IBundlingModule
    {
        private readonly Action<BundleDefaultsOptions, IServiceProvider> _configureDefaults;

        public SassBundlingModule() : this(null) { }

        public SassBundlingModule(Action<BundleDefaultsOptions, IServiceProvider> configureDefaults)
        {
            _configureDefaults = configureDefaults;
        }

        public BundlingConfigurer Configure(BundlingConfigurer configurer)
        {
            return configurer.AddSass(_configureDefaults);
        }
    }
}
