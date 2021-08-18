using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Karambolo.AspNetCore.Bundling.Internal.Caching
{
    public class NullBundleCache : IBundleCache
    {
        private readonly ILogger _logger;
        private int _hasLoggedWarningFlag;

        public NullBundleCache(ILogger<NullBundleCache> logger)
        {
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            _logger = logger;

        }
        public async Task<IBundleCacheItem> GetOrAddAsync(BundleCacheKey key, Func<CancellationToken, Task<BundleCacheData>> factory, CancellationToken token, IBundleCacheOptions cacheOptions, bool lockFile = false)
        {
            if (Interlocked.CompareExchange(ref _hasLoggedWarningFlag, 1, 0) == 0)
                _logger.LogWarning($"Bundles are not cached but built on every request because no actual implementation of the {nameof(IBundleCache)} interface was registered. Register an actual cache implementation by calling either the {nameof(BundlingServiceCollectionExtensions.UseMemoryCaching)} or the {nameof(BundlingServiceCollectionExtensions.UseFileSystemCaching)} builder method in your Startup.ConfigureServices method.");

            BundleCacheData data = await factory(token);
            var fileInfo = new MemoryFileInfo(Path.GetFileName(key.Path), data.Content, data.Timestamp);
            return new MemoryBundleCache.Item(fileInfo, data);
        }

        public Task RemoveAllAsync(int managerId, PathString bundlePath, CancellationToken token)
        {
            return Task.CompletedTask;
        }

        public Task RemoveAsync(BundleCacheKey key, CancellationToken token)
        {
            return Task.CompletedTask;
        }
    }
}
