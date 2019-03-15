using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Primitives;
using Karambolo.AspNetCore.Bundling.Internal.Helpers;
using System.Threading;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

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
        static readonly object httpContextItemKey = new object();

        readonly int _id;
        readonly IBundlingContext _bundlingContext;
        readonly Dictionary<PathString, IBundleModel> _bundles;
        readonly CancellationToken _shutdownToken;

        readonly IEnumerable<IBundleModelFactory> _modelFactories;
        readonly IBundleCache _cache;
        readonly IBundleVersionProvider _versionProvider;
        readonly IBundleUrlHelper _urlHelper;

        readonly ILogger _logger;
        readonly ISystemClock _clock;

        public BundleManager(int id, BundleCollection bundles, IBundlingContext bundlingContext, CancellationToken shutdownToken,
            IEnumerable<IBundleModelFactory> modelFactories, IBundleCache cache, IBundleVersionProvider versionProvider, IBundleUrlHelper urlHelper,
            ILoggerFactory loggerFactory, ISystemClock clock)
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

            _bundles = bundles.ToDictionary(b => b.Path, CreateModel);
        }

        protected virtual IBundleModel CreateModel(Bundle bundle)
        {
            var result =
                _modelFactories.Select(f => f.Create(bundle)).FirstOrDefault(m => m != null) ??
                throw ErrorHelper.ModelFactoryNotAvailable(bundle.GetType());

            result.Changed += BundleChanged;
            return result;
        }

        protected virtual void BundleChanged(object sender, EventArgs e)
        {
            _cache.RemoveAllAsync(_id, ((IBundleModel)sender).Path, _shutdownToken).GetAwaiter().GetResult();
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
            };

            await bundle.Builder.BuildAsync(builderContext);

            var content = bundle.OutputEncoding.GetBytes(builderContext.Result);
            var timestamp = _clock.UtcNow;

            var versionProviderContext = new BundleVersionProviderContext
            {
                HttpContext = httpContext,
                Timestamp = timestamp,
                Content = content,
            };

            _versionProvider.Provide(versionProviderContext);

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
            var pathPrefix = httpContext.Request.PathBase + _bundlingContext.BundlesPathPrefix;

            if (!path.StartsWithSegments(pathPrefix, out PathString bundlePath) ||
                !_bundles.TryGetValue(bundlePath, out IBundleModel bundle))
                return null;

            query = UrlUtils.NormalizeQuery(query, out IDictionary<string, StringValues> @params);
            if (!bundle.DependsOnParams)
                query = QueryString.Empty;

            var cacheKey = new BundleCacheKey(_id, bundlePath, query);
            var cacheItem = await _cache.GetOrAddAsync(cacheKey, ct => BuildBundleAsync(bundle, query, @params, httpContext),
                httpContext.RequestAborted, bundle.CacheOptions);

            _urlHelper.AddVersion(cacheItem.Version, ref bundlePath, ref query);

            return pathPrefix + bundlePath + query;
        }

        public async Task<bool> TryEnsureUrlAsync(HttpContext httpContext)
        {
            var branchPath = httpContext.Request.Path;
            if (!branchPath.StartsWithSegments(_bundlingContext.BundlesPathPrefix, out PathString bundlePath))
                return false;

            var query = httpContext.Request.QueryString;
            _urlHelper.RemoveVersion(ref bundlePath, ref query);

            if (!_bundles.TryGetValue(bundlePath, out IBundleModel bundle))
                return false;

            var disposer = httpContext.RequestServices.GetRequiredService<IScopedDisposer>();

            query = UrlUtils.NormalizeQuery(query, out IDictionary<string, StringValues> @params);
            if (!bundle.DependsOnParams)
                query = QueryString.Empty;

            var cacheKey = new BundleCacheKey(_id, bundlePath, query);
            var cacheItem = await _cache.GetOrAddAsync(cacheKey, ct => BuildBundleAsync(bundle, query, @params, httpContext),
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
            httpContext.Items.Add(httpContextItemKey, cacheItem.FileInfo);

            return true;
        }

        public IFileInfo GetFileInfo(HttpContext httpContext)
        {
            return
                httpContext.Items.TryGetValue(httpContextItemKey, out object fileInfo) ?
                (IFileInfo)fileInfo :
                throw ErrorHelper.BundleInfoNotAvailable(httpContext.Request.Path, httpContext.Request.QueryString);
        }
    }
}
