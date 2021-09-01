using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Karambolo.AspNetCore.Bundling.Internal.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Karambolo.AspNetCore.Bundling.Internal
{
    public interface IBundleManager
    {
        int Id { get; }
        IBundlingContext BundlingContext { get; }

        bool TryGetBundle(HttpContext httpContext, PathString path, out IBundleModel bundle);
        Task<IBundleSourceBuildItem[]> GetBuildItemsAsync(HttpContext httpContext, IBundleModel bundle, QueryString query, bool loadItemContent = true);
        Task<string> GenerateUrlAsync(HttpContext httpContext, IBundleModel bundle, QueryString query, bool addVersion = true);
        Task<bool> TryEnsureUrlAsync(HttpContext httpContext);
        IFileInfo GetFileInfo(HttpContext httpContext);
    }

    public class BundleManager : IBundleManager
    {
        private static readonly object s_httpContextItemKey = new object();
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
            ILogger<BundleManager> logger, ISystemClock clock, IOptions<BundleGlobalOptions> globalOptions)
        {
            Id = id;
            BundlingContext = bundlingContext;

            _shutdownToken = shutdownToken;

            _modelFactories = modelFactories;
            _cache = cache;
            _versionProvider = versionProvider;
            _urlHelper = urlHelper;

            _logger = logger;
            _clock = clock;

            _enableChangeDetection = globalOptions.Value.EnableChangeDetection;

            Bundles = bundles.ToDictionary(b => b.Path, CreateModel);
        }

        public int Id { get; }
        public IBundlingContext BundlingContext { get; }

        protected Dictionary<PathString, IBundleModel> Bundles { get; }

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
            try { await _cache.RemoveAllAsync(Id, bundle.Path, _shutdownToken); }
            catch (Exception ex) { _logger.LogError(ex, "Unexpected error occurred during updating cache."); }
        }

        protected virtual void BundleChanged(object sender, EventArgs e)
        {
            InvalidateBundleCache((IBundleModel)sender);
        }

        protected virtual async Task<BundleCacheData> BuildBundleAsync(IBundleModel bundle, QueryString query, IDictionary<string, StringValues> @params, HttpContext httpContext)
        {
            var builderContext = new BundleBuilderContext
            {
                BundlingContext = BundlingContext,
                AppBasePath = httpContext.Request.PathBase,
                Params = @params,
                Bundle = bundle,
                ChangeSources = _enableChangeDetection ? new HashSet<IChangeSource>() : null,
                CancellationToken = httpContext.RequestAborted
            };

            bundle.OnBuilding(builderContext);

            await bundle.Builder.BuildAsync(builderContext);

            var content = bundle.OutputEncoding.GetBytes(builderContext.Result);
            DateTimeOffset timestamp = _clock.UtcNow;

            var versionProviderContext = new BundleVersionProviderContext
            {
                Timestamp = timestamp,
                Content = content,
                CancellationToken = httpContext.RequestAborted
            };

            _versionProvider.Provide(versionProviderContext);

            bundle.OnBuilt(builderContext);

            return new BundleCacheData
            {
                Content = content,
                Timestamp = timestamp,
                Version = versionProviderContext.Result,
            };
        }

        private Task<IBundleCacheItem> GetBundleCacheItemAsync(BundleCacheKey cacheKey, IBundleModel bundle, QueryString query, IDictionary<string, StringValues> @params, HttpContext httpContext,
            bool lockFile)
        {
            return _cache.GetOrAddAsync(
                cacheKey,
                async _ =>
                {
                    long startTicks = Stopwatch.GetTimestamp();

                    BundleCacheData cacheItem;
                    try { cacheItem = await BuildBundleAsync(bundle, query, @params, httpContext); }
                    catch (OperationCanceledException)
                    {
                        _logger.LogInformation("Bundle [{MANAGER_ID}]:{PATH}{QUERY} was not built. Build was cancelled.", Id, bundle.Path, query);
                        throw;
                    }
                    catch
                    {
                        _logger.LogInformation("Bundle [{MANAGER_ID}]:{PATH}{QUERY} was not built. Build failed.", Id, bundle.Path, query);
                        throw;
                    }

                    long endTicks = Stopwatch.GetTimestamp();

                    if (_logger.IsEnabled(LogLevel.Information))
                    {
                        long elapsedMs = (endTicks - startTicks) / (Stopwatch.Frequency / 1000);
                        _logger.LogInformation("Bundle [{MANAGER_ID}]:{PATH}{QUERY} was built in {ELAPSED}ms.", Id, bundle.Path, query, elapsedMs);
                    }

                    return cacheItem;
                },
                httpContext.RequestAborted, bundle.CacheOptions, lockFile);
        }

        public bool TryGetBundle(HttpContext httpContext, PathString path, out IBundleModel bundle)
        {
            if (path.StartsWithSegments(httpContext.Request.PathBase, out path) &&
                path.StartsWithSegments(BundlingContext.BundlesPathPrefix, out path) &&
                Bundles.TryGetValue(path, out bundle))
            {
                return true;
            }

            bundle = default;
            return false;
        }

        public async Task<IBundleSourceBuildItem[]> GetBuildItemsAsync(HttpContext httpContext, IBundleModel bundle, QueryString query, bool loadItemContent = true)
        {
            UrlUtils.NormalizeQuery(query, out IDictionary<string, StringValues> @params);

            var provideBuildItemsContext = new BundleProvideBuildItemsContext
            {
                BundlingContext = BundlingContext,
                AppBasePath = httpContext.Request.PathBase,
                Params = @params,
                Bundle = bundle,
                CancellationToken = httpContext.RequestAborted,
                LoadItemContent = loadItemContent
            };

            var items = new ConcurrentQueue<IBundleSourceBuildItem>();

            for (int i = 0, n = provideBuildItemsContext.Bundle.Sources.Length; i < n; i++)
            {
                provideBuildItemsContext.CancellationToken.ThrowIfCancellationRequested();

                IBundleSourceModel source = provideBuildItemsContext.Bundle.Sources[i];
                await source.ProvideBuildItemsAsync(provideBuildItemsContext, items.Enqueue);
            }

            return items.ToArray();
        }

        public async Task<string> GenerateUrlAsync(HttpContext httpContext, IBundleModel bundle, QueryString query, bool addVersion = true)
        {
            QueryString normalizedQuery;
            IDictionary<string, StringValues> @params;

            if (!bundle.DependsOnParams)
                (normalizedQuery, @params) = (QueryString.Empty, null);
            else
                normalizedQuery = UrlUtils.NormalizeQuery(query, out @params);

            PathString bundlePath = bundle.Path;

            if (addVersion)
            {
                var cacheKey = new BundleCacheKey(Id, bundlePath, normalizedQuery);
                IBundleCacheItem cacheItem = await GetBundleCacheItemAsync(cacheKey, bundle, query, @params, httpContext, lockFile: false);

                _urlHelper.AddVersion(cacheItem.Version, ref bundlePath, ref query);
            }

            return httpContext.Request.PathBase + BundlingContext.BundlesPathPrefix + bundlePath + query;
        }

        public async Task<bool> TryEnsureUrlAsync(HttpContext httpContext)
        {
            if (!httpContext.Request.Path.StartsWithSegments(BundlingContext.BundlesPathPrefix, out PathString bundlePath))
                return false;

            QueryString query = httpContext.Request.QueryString;
            _urlHelper.RemoveVersion(ref bundlePath, ref query);

            if (!Bundles.TryGetValue(bundlePath, out IBundleModel bundle))
                return false;

            QueryString normalizedQuery;
            IDictionary<string, StringValues> @params;

            if (!bundle.DependsOnParams)
                (normalizedQuery, @params) = (QueryString.Empty, null);
            else
                normalizedQuery = UrlUtils.NormalizeQuery(query, out @params);

            var cacheKey = new BundleCacheKey(Id, bundlePath, normalizedQuery);
            IBundleCacheItem cacheItem = await GetBundleCacheItemAsync(cacheKey, bundle, query, @params, httpContext, lockFile: true);

            try
            {
                // scheduling release of the lock for the end of the request so that the file remain unchanged until it's served
                httpContext.ScheduleDisposeForRequestEnd(cacheItem.FileReleaser);
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
