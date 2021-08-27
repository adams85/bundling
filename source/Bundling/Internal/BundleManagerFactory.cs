using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Karambolo.AspNetCore.Bundling.Internal
{
#if !NETCOREAPP3_0_OR_GREATER
    using IHostApplicationLifetime = Microsoft.AspNetCore.Hosting.IApplicationLifetime;
#else
    using Microsoft.Extensions.Hosting;
#endif

    public interface IBundleManagerFactory
    {
        IBundleManager Create(BundleCollection bundles, IBundlingContext bundlingContext);
        IReadOnlyList<IBundleManager> Instances { get; }
    }

    public class BundleManagerFactory : IBundleManagerFactory
    {
        private readonly IEnumerable<IBundleModelFactory> _modelFactories;
        private readonly IBundleCache _cache;
        private readonly IBundleVersionProvider _versionProvider;
        private readonly IBundleUrlHelper _urlHelper;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ISystemClock _clock;
        private readonly List<IBundleManager> _instances;
        private readonly CancellationToken _shutdownToken;
        private readonly IOptions<BundleGlobalOptions> _globalOptions;

        public BundleManagerFactory(IEnumerable<IBundleModelFactory> modelFactories, IBundleCache cache, IBundleVersionProvider versionProvider, IBundleUrlHelper urlHelper,
            ILoggerFactory loggerFactory, ISystemClock clock, IHostApplicationLifetime applicationLifetime, IOptions<BundleGlobalOptions> globalOptions)
        {
            _modelFactories = modelFactories;
            _cache = cache;
            _versionProvider = versionProvider;
            _urlHelper = urlHelper;

            _loggerFactory = loggerFactory;
            _clock = clock;

            _shutdownToken = applicationLifetime.ApplicationStopping;

            _globalOptions = globalOptions;

            _instances = new List<IBundleManager>();
        }

        public IReadOnlyList<IBundleManager> Instances => _instances;

        public IBundleManager Create(BundleCollection bundles, IBundlingContext bundlingContext)
        {
            var result = new BundleManager(
                _instances.Count, bundles, bundlingContext, _shutdownToken, _modelFactories, _cache, _versionProvider, _urlHelper,
                _loggerFactory.CreateLogger<BundleManager>(), _clock, _globalOptions);

            _instances.Add(result);
            return result;
        }
    }
}
