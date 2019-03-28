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

        public DefaultBundleModelFactory(Lazy<IEnumerable<IBundleModelFactory>> modelFactories, IApplicationLifetime appLifetime)
        {
            _modelFactories = modelFactories;
            _appLifetime = appLifetime;
        }

        public IBundleModel Create(Bundle bundle)
        {
            if (bundle == null)
                throw new ArgumentNullException(nameof(bundle));

            return _appLifetime.ScheduleDisposeForShutdown(new DefaultBundleModel(bundle, _modelFactories.Value));
        }

        public IBundleSourceModel CreateSource(BundleSource bundleSource)
        {
            if (bundleSource == null)
                throw new ArgumentNullException(nameof(bundleSource));

            if (bundleSource is FileBundleSource fileBundleSource)
                return new FileBundleSourceModel(fileBundleSource);

            if (bundleSource is DynamicBundleSource dynamicBundleSource)
                return new DynamicBundleSourceModel(dynamicBundleSource);

            return null;
        }
    }
}
