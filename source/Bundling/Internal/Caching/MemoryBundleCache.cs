using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Karambolo.AspNetCore.Bundling.Internal.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Karambolo.AspNetCore.Bundling.Internal.Caching
{
    public class MemoryBundleCache : IBundleCache, IDisposable
    {
        internal sealed class Item : IBundleCacheItem
        {
            public Item(IFileInfo fileInfo, BundleCacheData data)
            {
                FileInfo = fileInfo;
                Version = data.Version;
            }

            public IFileInfo FileInfo { get; }
            public DateTimeOffset Timestamp => FileInfo.LastModified;
            public string Version { get; }
            public IDisposable FileReleaser => NullDisposable.Instance;
        }

        private readonly ConcurrentDictionary<(int, PathString), CancellationTokenSource> _changeTokenSources;
        private readonly IMemoryCache _cache;

        public MemoryBundleCache(IMemoryCache cache, IOptions<BundleGlobalOptions> globalOptions)
        {
            _cache = cache;

            if (globalOptions.Value.EnableChangeDetection)
                _changeTokenSources = new ConcurrentDictionary<(int, PathString), CancellationTokenSource>();
        }

        public void Dispose()
        {
            if (_changeTokenSources != null)
                foreach (CancellationTokenSource cts in _changeTokenSources.Values)
                    cts.Dispose();
        }

        private Item CreateCacheItem(BundleCacheKey key, BundleCacheData data)
        {
            var fileInfo = new MemoryFileInfo(Path.GetFileName(key.Path), data.Content, data.Timestamp);
            return new Item(fileInfo, data);
        }

        public async Task<IBundleCacheItem> GetOrAddAsync(BundleCacheKey key, Func<CancellationToken, Task<BundleCacheData>> factory, CancellationToken token,
            IBundleCacheOptions cacheOptions, bool lockFile = false)
        {
            // lockFile is ignored since FileInfo holds the file content, no underlying resource needs to be locked

            if (cacheOptions.NoCache)
                return CreateCacheItem(key, await factory(token));

            Lazy<Task<Item>> factoryTask = _cache.GetOrCreate(key, ce =>
            {
                if (_changeTokenSources != null)
                {
                    CancellationTokenSource cts = _changeTokenSources.GetOrAdd((key.ManagerId, key.Path), p => new CancellationTokenSource());
                    ce.AddExpirationToken(new CancellationChangeToken(cts.Token));
                }

                ce.AbsoluteExpiration = cacheOptions.AbsoluteExpiration;
                ce.SlidingExpiration = cacheOptions.SlidingExpiration;
                ce.Priority = cacheOptions.Priority;

                return new Lazy<Task<Item>>(async () => CreateCacheItem(key, await factory(token)), LazyThreadSafetyMode.ExecutionAndPublication);
            });

            try
            {
                return await factoryTask.Value;
            }
            catch
            {
                _cache.Remove(key);
                throw;
            }
        }

        public Task RemoveAsync(BundleCacheKey key, CancellationToken token)
        {
            _cache.Remove(key);
            return Task.CompletedTask;
        }

        public Task RemoveAllAsync(int managerId, PathString bundlePath, CancellationToken token)
        {
            if (_changeTokenSources == null)
                throw ErrorHelper.ChangeDetectionNotEnabled();

            if (_changeTokenSources.TryRemove((managerId, bundlePath), out CancellationTokenSource cts))
            {
                cts.Cancel();
                cts.Dispose();
            }

            return Task.CompletedTask;
        }
    }
}
