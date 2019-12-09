using System;
using System.Collections.Generic;
using Karambolo.AspNetCore.Bundling.Internal.Helpers;
using Karambolo.AspNetCore.Bundling.Internal.Models;

namespace Karambolo.AspNetCore.Bundling.Internal
{
#if NETSTANDARD2_0
    using IHostApplicationLifetime = Microsoft.AspNetCore.Hosting.IApplicationLifetime;
#else
    using Microsoft.Extensions.Hosting;
#endif

    public class DefaultBundleModelFactory : IBundleModelFactory
    {
        private readonly Lazy<IEnumerable<IBundleModelFactory>> _modelFactories;
        private readonly IHostApplicationLifetime _appLifetime;

        public DefaultBundleModelFactory(Lazy<IEnumerable<IBundleModelFactory>> modelFactories, IHostApplicationLifetime appLifetime)
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
