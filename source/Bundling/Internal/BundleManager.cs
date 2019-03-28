using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Karambolo.AspNetCore.Bundling.Internal.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Karambolo.AspNetCore.Bundling.Internal
{
    public interface IBundleManager
    {
        Task<string> TryGenerateUrlAsync(PathString path, QueryString query, HttpContext httpContext);
        Task<bool> TryEnsureUrlAsync(HttpContext httpContext);
        IFileInfo GetFileInfo(HttpContext httpContext);
    }

    public class BundleManager : IBundleManager
    {
        private static readonly object s_httpContextItemKey = new object();
        private readonly int _id;
        private readonly IBundlingContext _bundlingContext;
        private readonly Dictionary<PathString, IBundleModel> _bundles;
        private readonly CancellationToken _shutdownToken;
        private readonly IEnumerable<IBundleModelFactory> _modelFactories;
        private readonly IBundleCache _cache;
        private readonly IBundleVersionProvider _versionProvider;
        private readonly IBundleUrlHelper _urlHelper;
        private readonly ILogger _logger;
        private readonly ISystemClock _clock;
        private readonly bool _enableChangeDetection;

        public BundleManager(int id, BundleCollection bundles, IBundlingContext bundlingContext, CancellationToken shutdownToken,
            IEnumerable<IBundleModelFactory> modelFactories, IBundleCache cache, IBundleVersionProvider versionProvider, IBundleUrlHelper urlHelper,
            ILoggerFactory loggerFactory, ISystemClock clock, IOptions<BundleGlobalOptions> globalOptions)
        {
            _id = id;
            _bundlingContext = bundlingContext;
            _shutdownToken = shutdownToken;

            _modelFactories = modelFactories;
            _cache = cache;
            _versionProvider = versionProvider;
            _urlHelper = urlHelper;

            _logger = loggerFactory.CreateLogger<BundleManager>();
            _clock = clock;

            _enableChangeDetection = globalOptions.Value.EnableChangeDetection;

            _bundles = bundles.ToDictionary(b => b.Path, CreateModel);
        }

        protected virtual IBundleModel CreateModel(Bundle bundle)
        {
            IBundleModel result =
                _modelFactories.Select(f => f.Create(bundle)).FirstOrDefault(m => m != null) ??
                throw ErrorHelper.ModelFactoryNotAvailable(bundle.GetType());

            result.Changed += BundleChanged;
            return result;
        }

        private async void InvalidateBundleCache(IBundleModel bundle)
        {
            try { await _cache.RemoveAllAsync(_id, bundle.Path, _shutdownToken).ConfigureAwait(false); }
            catch (Exception ex) { _logger.LogError(ex, "Unexpected error occurred during updating cache."); }
        }

        protected virtual void BundleChanged(object sender, EventArgs e)
        {
            InvalidateBundleCache((IBundleModel)sender);
        }

        protected virtual async Task<BundleCacheData> BuildBundleAsync(IBundleModel bundle, QueryString query, IDictionary<string, StringValues> @params, HttpContext httpContext)
        {
            var startTicks = Stopwatch.GetTimestamp();

            var builderContext = new BundleBuilderContext
            {
                BundlingContext = _bundlingContext,
                HttpContext = httpContext,
                Params = @params,
                Bundle = bundle,
                ChangeSources = _enableChangeDetection ? new HashSet<IChangeSource>() : null
            };

            bundle.OnBuilding(builderContext);

            await bundle.Builder.BuildAsync(builderContext);

            var content = bundle.OutputEncoding.GetBytes(builderContext.Result);
            DateTimeOffset timestamp = _clock.UtcNow;

            var versionProviderContext = new BundleVersionProviderContext
            {
                HttpContext = httpContext,
                Timestamp = timestamp,
                Content = content,
            };

            _versionProvider.Provide(versionProviderContext);

            bundle.OnBuilt(builderContext);

            var endTicks = Stopwatch.GetTimestamp();

            if (_logger.IsEnabled(LogLevel.Information))
            {
                var elapsedMs = (endTicks - startTicks) / (Stopwatch.Frequency / 1000);
                _logger.LogInformation("Bundle instance [{MANAGER_ID}]:{PATH}{QUERY} was built in {ELAPSED}ms.", _id, bundle.Path, query, elapsedMs);
            }

            return new BundleCacheData
            {
                Content = content,
                Timestamp = timestamp,
                Version = versionProviderContext.Result,
            };
        }

        public async Task<string> TryGenerateUrlAsync(PathString path, QueryString query, HttpContext httpContext)
        {
            PathString pathPrefix = httpContext.Request.PathBase + _bundlingContext.BundlesPathPrefix;

            if (!path.StartsWithSegments(pathPrefix, out PathString bundlePath) ||
                !_bundles.TryGetValue(bundlePath, out IBundleModel bundle))
                return null;

            query = UrlUtils.NormalizeQuery(query, out IDictionary<string, StringValues> @params);
            if (!bundle.DependsOnParams)
                query = QueryString.Empty;

            var cacheKey = new BundleCacheKey(_id, bundlePath, query);
            IBundleCacheItem cacheItem = await _cache.GetOrAddAsync(cacheKey, ct => BuildBundleAsync(bundle, query, @params, httpContext),
                httpContext.RequestAborted, bundle.CacheOptions);

            _urlHelper.AddVersion(cacheItem.Version, ref bundlePath, ref query);

            return pathPrefix + bundlePath + query;
        }

        public async Task<bool> TryEnsureUrlAsync(HttpContext httpContext)
        {
            PathString branchPath = httpContext.Request.Path;
            if (!branchPath.StartsWithSegments(_bundlingContext.BundlesPathPrefix, out PathString bundlePath))
                return false;

            QueryString query = httpContext.Request.QueryString;
            _urlHelper.RemoveVersion(ref bundlePath, ref query);

            if (!_bundles.TryGetValue(bundlePath, out IBundleModel bundle))
                return false;

            IScopedDisposer disposer = httpContext.RequestServices.GetRequiredService<IScopedDisposer>();

            query = UrlUtils.NormalizeQuery(query, out IDictionary<string, StringValues> @params);
            if (!bundle.DependsOnParams)
                query = QueryString.Empty;

            var cacheKey = new BundleCacheKey(_id, bundlePath, query);
            IBundleCacheItem cacheItem = await _cache.GetOrAddAsync(cacheKey, ct => BuildBundleAsync(bundle, query, @params, httpContext),
                httpContext.RequestAborted, bundle.CacheOptions, lockFile: true);

            try
            {
                // scheduling release of the lock for the end of the scope (request),
                // so that the file remain unchanged until it's served
                disposer.Register(cacheItem.FileReleaser);
            }
            catch
            {
                cacheItem.FileReleaser.Dispose();
                throw;
            }

            // passing file info to GetFileInfo(), which is called later in the request (see BundlingMiddleware and BundleFileProvider)
            httpContext.Items.Add(s_httpContextItemKey, cacheItem.FileInfo);

            return true;
        }

        public IFileInfo GetFileInfo(HttpContext httpContext)
        {
            return
                httpContext.Items.TryGetValue(s_httpContextItemKey, out object fileInfo) ?
                (IFileInfo)fileInfo :
                throw ErrorHelper.BundleInfoNotAvailable(httpContext.Request.Path, httpContext.Request.QueryString);
        }
    }
}
