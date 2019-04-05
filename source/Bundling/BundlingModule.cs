using System;
using Microsoft.Extensions.DependencyInjection;

namespace Karambolo.AspNetCore.Bundling
{
    public interface IBundlingModule
    {
        BundlingConfigurer Configure(BundlingConfigurer configurer);
    }

    public sealed class CssBundlingModule : IBundlingModule
    {
        private readonly Action<BundleDefaultsOptions, IServiceProvider> _configureDefaults;

        public CssBundlingModule() : this(null) { }

        public CssBundlingModule(Action<BundleDefaultsOptions, IServiceProvider> configureDefaults)
        {
            _configureDefaults = configureDefaults;
        }

        public BundlingConfigurer Configure(BundlingConfigurer configurer)
        {
            return configurer.AddCss(_configureDefaults);
        }
    }

    public sealed class JsBundlingModule : IBundlingModule
    {
        private readonly Action<BundleDefaultsOptions, IServiceProvider> _configureDefaults;

        public JsBundlingModule() : this(null) { }

        public JsBundlingModule(Action<BundleDefaultsOptions, IServiceProvider> configureDefaults)
        {
            _configureDefaults = configureDefaults;
        }

        public BundlingConfigurer Configure(BundlingConfigurer configurer)
        {
            return configurer.AddJs(_configureDefaults);
        }
    }
}
