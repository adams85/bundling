using System;
using Karambolo.AspNetCore.Bundling;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder
{
    public class BundleCollectionConfigurer
    {
        private readonly IOptionsMonitor<BundleDefaultsOptions> _defaultsOptions;

        public BundleCollectionConfigurer(BundleCollection bundles, IServiceProvider appServices)
        {
            if (bundles == null)
                throw new ArgumentNullException(nameof(bundles));

            if (appServices == null)
                throw new ArgumentNullException(nameof(appServices));

            _defaultsOptions = appServices.GetRequiredService<IOptionsMonitor<BundleDefaultsOptions>>();

            Bundles = bundles;
            AppServices = appServices;
        }

        public BundleCollection Bundles { get; }
        public IServiceProvider AppServices { get; }

        public BundleDefaultsOptions GetDefaults(string bundleType)
        {
            if (bundleType == null)
                throw new ArgumentNullException(nameof(bundleType));

            return _defaultsOptions.Get(bundleType);
        }
    }
}
