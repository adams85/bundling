using System;
using System.Collections.Generic;
using Karambolo.AspNetCore.Bundling.Internal.Helpers;
using Karambolo.AspNetCore.Bundling.Internal.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace Karambolo.AspNetCore.Bundling.Internal
{
    public class DefaultBundleModelFactory : IBundleModelFactory
    {
        private readonly Lazy<IEnumerable<IBundleModelFactory>> _modelFactories;
        private readonly IApplicationLifetime _appLifetime;
        private readonly bool _enableChangeDetection;

        public DefaultBundleModelFactory(Lazy<IEnumerable<IBundleModelFactory>> modelFactories, IApplicationLifetime appLifetime, IOptions<BundleGlobalOptions> globalOptions)
        {
            _modelFactories = modelFactories;
            _appLifetime = appLifetime;
            _enableChangeDetection = globalOptions.Value.EnableChangeDetection;
        }

        public IBundleModel Create(Bundle bundle)
        {
            if (bundle == null)
                throw new ArgumentNullException(nameof(bundle));

            return new DefaultBundleModel(bundle, _modelFactories.Value);
        }

        public IBundleSourceModel CreateSource(BundleSource bundleSource)
        {
            if (bundleSource == null)
                throw new ArgumentNullException(nameof(bundleSource));

            if (bundleSource is FileBundleSource fileBundleSource)
                return _appLifetime.ScheduleDisposeForShutdown(new FileBundleSourceModel(fileBundleSource, _enableChangeDetection));

            if (bundleSource is DynamicBundleSource dynamicBundleSource)
                return _appLifetime.ScheduleDisposeForShutdown(new DynamicBundleSourceModel(dynamicBundleSource, _enableChangeDetection));

            return null;
        }
    }
}
