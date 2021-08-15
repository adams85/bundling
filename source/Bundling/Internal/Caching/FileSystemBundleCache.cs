using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Karambolo.AspNetCore.Bundling.Internal.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Karambolo.AspNetCore.Bundling.Internal.Caching
{
#if NETSTANDARD2_0
    using IWebHostEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;
#else
    using Microsoft.AspNetCore.Hosting;
#endif

    public class FileSystemBundleCacheOptions
    {
        public PhysicalFileProvider FileProvider { get; set; }
        public string BasePath { get; set; }
        public bool EnableExpirationScanning { get; set; } = true;
        public TimeSpan ExpirationScanFrequency { get; set; } = TimeSpan.FromMinutes(5);
        public bool AutoResetOnCreate { get; set; }
    }

    public class FileSystemBundleCache : IBundleCache
    {
        protected class StoreItemMetadata
        {
            public StoreItemMetadata() { }

            public StoreItemMetadata(string itemFileName, BundleCacheData data, IBundleCacheOptions cacheOptions)
            {
                ItemFileName = itemFileName;
                Timestamp = data.Timestamp;
                Version = data.Version;
                AbsoluteExpiration = cacheOptions.AbsoluteExpiration;
                SlidingExpiration = cacheOptions.SlidingExpiration;
            }

            public string ItemFileName { get; set; }
            public DateTimeOffset Timestamp { get; set; }
            public string Version { get; set; }
            public DateTimeOffset? AbsoluteExpiration { get; set; }
            public TimeSpan? SlidingExpiration { get; set; }
        }

        protected readonly struct StoreItem
        {
            public static readonly StoreItem NotAvailable = default;

            public StoreItem(IFileInfo fileInfo, StoreItemMetadata metadata, bool isExpired)
            {
                FileInfo = fileInfo;
                Timestamp = metadata.Timestamp;
                Version = metadata.Version;
                IsExpired = isExpired;
            }

            public StoreItem(IFileInfo fileInfo, BundleCacheData data)
            {
                FileInfo = fileInfo;
                Timestamp = data.Timestamp;
                Version = data.Version;
                IsExpired = false;
            }

            public IFileInfo FileInfo { get; }
            public DateTimeOffset Timestamp { get; }
            public string Version { get; }

            public bool IsAvailable => FileInfo != null;
            public bool IsExpired { get; }
        }

        private class Item : IBundleCacheItem
        {
            private readonly StoreItem _storeItem;

            public Item(StoreItem storeItem, IDisposable fileReleaser)
            {
                _storeItem = storeItem;
                FileReleaser = fileReleaser;
            }

            public IFileInfo FileInfo => _storeItem.FileInfo;
            public DateTimeOffset Timestamp => _storeItem.Timestamp;
            public string Version => _storeItem.Version;
            public IDisposable FileReleaser { get; }
        }

        private const int CopyBufferSize = 1024;

        private readonly TimeSpan _monitorScanFrequency;
        private readonly CancellationToken _shutdownToken;
        private readonly ISystemClock _clock;
        private readonly ILogger _logger;
        private readonly AsyncKeyedLock<(int, PathString)> _bundleLock;

        public FileSystemBundleCache(CancellationToken shutdownToken, IWebHostEnvironment env, ILogger<FileSystemBundleCache> logger, ISystemClock clock,
            IOptions<FileSystemBundleCacheOptions> options)
        {
            FileSystemBundleCacheOptions optionsUnwrapped = options.Value;
            FileProvider = optionsUnwrapped.FileProvider ?? env.ContentRootFileProvider as PhysicalFileProvider ?? throw ErrorHelper.ContentRootNotPhysical(nameof(options));
            BasePath = optionsUnwrapped.BasePath ?? @"App_Data\Bundles";

            _shutdownToken = shutdownToken;
            _clock = clock;
            _logger = logger;

            _bundleLock = new AsyncKeyedLock<(int, PathString)>();

            if (optionsUnwrapped.EnableExpirationScanning)
            {
                _monitorLock = new AsyncKeyedLock<object>();
                _acquireMonitorReadLock = () => _monitorLock.ReaderLockAsync(string.Empty);
                _acquireMonitorWriteLock = () => _monitorLock.WriterLockAsync(string.Empty);

                _monitorScanFrequency = optionsUnwrapped.ExpirationScanFrequency;
                Task.Run(() => MonitorAsync());
            }
            else
                _acquireMonitorReadLock = _acquireMonitorWriteLock = () => Task.FromResult<IDisposable>(NullDisposable.Instance);

            if (optionsUnwrapped.AutoResetOnCreate)
                Reset();
        }

        public PhysicalFileProvider FileProvider { get; }
        public string BasePath { get; }
        public string PhysicalBasePath => Path.Combine(FileProvider.Root, BasePath);

        public void Reset()
        {
            if (Directory.Exists(PhysicalBasePath))
                Directory.Delete(PhysicalBasePath, recursive: true);
        }

        protected virtual string MetadataFileNamePostfix => ".meta.json";
        protected virtual string TimestampFileNamePostfix => ".meta.ts";

        protected virtual string GetItemsBasePath(int managerId, string path)
        {
            return Path.Combine(BasePath, managerId.ToString(), UrlUtils.PathToFileName(path));
        }

        protected virtual string GetItemFileName(BundleCacheKey key)
        {
            return UrlUtils.QueryToFileName(key.Query.HasValue ? key.Query.ToString() : "?") + Path.GetExtension(key.Path);
        }

        private static bool TryGetSlidingExpiration(string filePath, out DateTimeOffset expirationTime)
        {
            try { expirationTime = File.GetLastWriteTimeUtc(filePath); }
            catch
            {
                expirationTime = default;
                return false;
            }

            return true;
        }

        private static bool TrySetSlidingExpiration(string filePath, DateTimeOffset expirationTime)
        {
            try { File.SetLastWriteTimeUtc(filePath, expirationTime.DateTime); }
            catch { return false; }

            return true;
        }

        private string GetPhysicalItemTimestampPath(StoreItemMetadata metadata, string physicalItemsBasePath)
        {
            return Path.Combine(physicalItemsBasePath, Path.ChangeExtension(metadata.ItemFileName, TimestampFileNamePostfix));
        }

        protected virtual void InitializeExpiration(StoreItemMetadata metadata, string physicalItemsBasePath)
        {
            if (metadata.SlidingExpiration != null)
                TrySetSlidingExpiration(GetPhysicalItemTimestampPath(metadata, physicalItemsBasePath), _clock.UtcNow + metadata.SlidingExpiration.Value);
        }

        protected virtual bool CheckExpiration(StoreItemMetadata metadata, string physicalItemsBasePath, bool update)
        {
            DateTimeOffset utcNow = _clock.UtcNow;

            if (metadata.AbsoluteExpiration != null && utcNow >= metadata.AbsoluteExpiration)
                return true;

            string physicalItemTimestampPath;
            if (metadata.SlidingExpiration != null &&
                TryGetSlidingExpiration(physicalItemTimestampPath = GetPhysicalItemTimestampPath(metadata, physicalItemsBasePath), out DateTimeOffset expirationTime))
            {
                if (_clock.UtcNow >= expirationTime)
                    return true;
                else if (update)
                    TrySetSlidingExpiration(physicalItemTimestampPath, utcNow + metadata.SlidingExpiration.Value);
            }

            return false;
        }

        protected virtual async Task<StoreItemMetadata> LoadItemMetadataAsync(string physicalItemMetadataPath, CancellationToken token)
        {
            using (var ms = new MemoryStream())
            {
                using (var fs = new FileStream(physicalItemMetadataPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    await fs.CopyToAsync(ms, CopyBufferSize, token);

                ms.Position = 0;
                return SerializationHelper.Deserialize<StoreItemMetadata>(new StreamReader(ms));
            }
        }

        protected virtual async Task<StoreItem> RetrieveItemAsync(BundleCacheKey key, CancellationToken token)
        {
            var itemsBasePath = GetItemsBasePath(key.ManagerId, key.Path);
            var itemFileName = GetItemFileName(key);
            var itemMetadataFileName = Path.ChangeExtension(itemFileName, MetadataFileNamePostfix);

            var physicalItemsBasePath = Path.Combine(FileProvider.Root, itemsBasePath);
            var physicalItemMetadataPath = Path.Combine(physicalItemsBasePath, itemMetadataFileName);
            var physicalItemPath = Path.Combine(physicalItemsBasePath, itemFileName);

            try
            {
                if (!File.Exists(physicalItemMetadataPath) || !File.Exists(physicalItemPath))
                    return StoreItem.NotAvailable;

                StoreItemMetadata metadata = await LoadItemMetadataAsync(physicalItemMetadataPath, token);

                IFileInfo fileInfo = FileProvider.GetFileInfo(Path.Combine(itemsBasePath, itemFileName));
                return new StoreItem(fileInfo, metadata, CheckExpiration(metadata, physicalItemsBasePath, update: true));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve bundle [{MANAGER_ID}]:{PATH}{QUERY} from the cache.", key.ManagerId, key.Path, key.Query);
                return StoreItem.NotAvailable;
            }
        }

        protected virtual async Task SaveItemMetadataAsync(string physicalItemMetadataPath, StoreItemMetadata metadata, CancellationToken token)
        {
            using (var ms = new MemoryStream())
            {
                var writer = new StreamWriter(ms);
                SerializationHelper.Serialize(writer, metadata);
                writer.Flush();

                ms.Position = 0;
                using (var fs = new FileStream(physicalItemMetadataPath, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    await ms.CopyToAsync(fs, CopyBufferSize, token);
                    await fs.FlushAsync(token);
                }
            }
        }

        protected virtual async Task<StoreItem> StoreItemAsync(BundleCacheKey key, BundleCacheData data, IBundleCacheOptions cacheOptions, CancellationToken token)
        {
            var itemsBasePath = GetItemsBasePath(key.ManagerId, key.Path);
            var itemFileName = GetItemFileName(key);
            var itemMetadataFileName = Path.ChangeExtension(itemFileName, MetadataFileNamePostfix);

            var physicalItemsBasePath = Path.Combine(FileProvider.Root, itemsBasePath);
            var physicalItemMetadataPath = Path.Combine(physicalItemsBasePath, itemMetadataFileName);
            var physicalItemPath = itemFileName != null ? Path.Combine(physicalItemsBasePath, itemFileName) : null;

            try
            {
                if (File.Exists(physicalItemMetadataPath) || File.Exists(physicalItemPath))
                    _logger.LogWarning("Bundle [{MANAGER_ID}]:{PATH}{QUERY} exists unexpectedly in the cache. Trying to overwrite it.",
                        key.ManagerId, key.Path, key.Query);

                if (!Directory.Exists(physicalItemsBasePath))
                    Directory.CreateDirectory(physicalItemsBasePath);

                var metadata = new StoreItemMetadata(itemFileName, data, cacheOptions);

                await SaveItemMetadataAsync(physicalItemMetadataPath, metadata, token);

                using (var fs = new FileStream(physicalItemPath, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    await fs.WriteAsync(data.Content, 0, data.Content.Length, token);
                    await fs.FlushAsync(token);
                }

                if (cacheOptions.SlidingExpiration != null)
                    using (new FileStream(GetPhysicalItemTimestampPath(metadata, physicalItemsBasePath), FileMode.Create, FileAccess.Write, FileShare.Read)) { }

                InitializeExpiration(metadata, physicalItemsBasePath);

                IFileInfo fileInfo = itemFileName != null ? FileProvider.GetFileInfo(Path.Combine(itemsBasePath, itemFileName)) : null;
                return new StoreItem(fileInfo, metadata, isExpired: false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to store bundle [{MANAGER_ID}]:{PATH}{QUERY} into the cache.", key.ManagerId, key.Path, key.Query);
                return StoreItem.NotAvailable;
            }
        }

        protected virtual IBundleCacheItem CreateMemoryCacheItem(BundleCacheKey key, BundleCacheData data)
        {
            var fileInfo = new MemoryFileInfo(Path.GetFileName(key.Path), data.Content, data.Timestamp);
            return new Item(new StoreItem(fileInfo, data), NullDisposable.Instance);
        }

        public async Task<IBundleCacheItem> GetOrAddAsync(BundleCacheKey key, Func<CancellationToken, Task<BundleCacheData>> factory, CancellationToken token,
            IBundleCacheOptions cacheOptions, bool lockFile = false)
        {
            if (cacheOptions.NoCache)
                return CreateMemoryCacheItem(key, await factory(token));

            (int ManagerId, PathString Path) lockKey = (key.ManagerId, key.Path);

            IDisposable monitorReleaser = await _acquireMonitorReadLock();
            try
            {
                Task<IDisposable> readerLockTask = _bundleLock.ReaderLockAsync(lockKey);
                IDisposable readerReleaser = await readerLockTask;
                try
                {
                    StoreItem storeItem = await RetrieveItemAsync(key, token);
                    if (!storeItem.IsAvailable || storeItem.IsExpired)
                    {
                        Task<IDisposable> writerLockTask = _bundleLock.WriterLockAsync(lockKey);
                        readerReleaser.Dispose();

                        using (await writerLockTask)
                        {
                            // double-checking because multiple writers may have been queuing
                            storeItem = await RetrieveItemAsync(key, token);

                            if (!storeItem.IsAvailable)
                            {
                                BundleCacheData data = await factory(token);

                                storeItem = await StoreItemAsync(key, data, cacheOptions, token);

                                // returning a non-cached item in the worst case
                                if (!storeItem.IsAvailable)
                                    return CreateMemoryCacheItem(key, data);
                            }

                            readerLockTask = _bundleLock.ReaderLockAsync(lockKey);
                        }

                        readerReleaser = await readerLockTask;
                    }

                    IDisposable fileReleaser;
                    if (lockFile)
                    {
                        // retaining both locks and leave releasing them to the caller
                        fileReleaser = new CompositeDisposable(readerReleaser, monitorReleaser);
                        readerReleaser = monitorReleaser = NullDisposable.Instance;
                    }
                    else
                        fileReleaser = NullDisposable.Instance;

                    return new Item(storeItem, fileReleaser);
                }
                finally { readerReleaser.Dispose(); }
            }
            finally { monitorReleaser.Dispose(); }
        }

        protected virtual Task DeleteItemAsync(string physicalItemsBasePath, string itemFileName, CancellationToken token)
        {
            var physicalItemPath = Path.Combine(physicalItemsBasePath, itemFileName);
            if (File.Exists(physicalItemPath))
                File.Delete(physicalItemPath);

            var physicalItemMetadataPath = Path.Combine(physicalItemsBasePath, Path.ChangeExtension(itemFileName, MetadataFileNamePostfix));
            if (File.Exists(physicalItemMetadataPath))
                File.Delete(physicalItemMetadataPath);

            var physicalTimestampPath = Path.Combine(physicalItemsBasePath, Path.ChangeExtension(itemFileName, TimestampFileNamePostfix));
            if (File.Exists(physicalTimestampPath))
                File.Delete(physicalTimestampPath);

            return Task.CompletedTask;
        }

        protected virtual async Task RemoveItemAsync(BundleCacheKey key, CancellationToken token)
        {
            var itemsBasePath = GetItemsBasePath(key.ManagerId, key.Path);
            var physicalItemsBasePath = Path.Combine(FileProvider.Root, itemsBasePath);

            var itemFileName = GetItemFileName(key);

            try
            {
                await DeleteItemAsync(physicalItemsBasePath, itemFileName, token);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to remove bundle [{MANAGER_ID}]:{PATH}{QUERY} from the cache.", key.ManagerId, key.Path, key.Query);
            }
        }

        public async Task RemoveAsync(BundleCacheKey key, CancellationToken token)
        {
            (int ManagerId, PathString Path) lockKey = (key.ManagerId, key.Path);

            using (await _acquireMonitorReadLock())
            using (await _bundleLock.WriterLockAsync(lockKey))
                await RemoveItemAsync(key, token);
        }

        protected virtual Task RemoveAllItemsAsync(int managerId, PathString bundlePath, CancellationToken token)
        {
            var itemsBasePath = GetItemsBasePath(managerId, bundlePath);
            var physicalItemsBasePath = Path.Combine(FileProvider.Root, itemsBasePath);

            try
            {
                if (Directory.Exists(physicalItemsBasePath))
                    Directory.Delete(physicalItemsBasePath, recursive: true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to remove bundle [{MANAGER_ID}]:{PATH} from the cache.", managerId, bundlePath);
            }

            return Task.CompletedTask;
        }

        public async Task RemoveAllAsync(int managerId, PathString bundlePath, CancellationToken token)
        {
            (int managerId, PathString bundlePath) lockKey = (managerId, bundlePath);

            using (await _acquireMonitorReadLock())
            using (await _bundleLock.WriterLockAsync(lockKey))
                await RemoveAllItemsAsync(managerId, bundlePath, token);
        }

#region Expiration scanning

        private readonly AsyncKeyedLock<object> _monitorLock;
        private readonly Func<Task<IDisposable>> _acquireMonitorWriteLock;
        private readonly Func<Task<IDisposable>> _acquireMonitorReadLock;

        private async Task MonitorAsync()
        {
            while (true)
                try
                {
                    await Task.Delay(_monitorScanFrequency, _shutdownToken);

                    using (await _acquireMonitorWriteLock())
                        await MonitorCoreAsync(_shutdownToken);
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    _logger.LogError(ex, "Unexpected error occurred during monitoring.");
                }
        }

        protected virtual async Task MonitorCoreAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            var metadataFileNamePostfix = MetadataFileNamePostfix;
            var files = Directory.GetFiles(PhysicalBasePath, "*" + metadataFileNamePostfix, SearchOption.AllDirectories);

            for (int i = 0, n = files.Length; i < n; i++)
            {
                token.ThrowIfCancellationRequested();

                var physicalItemMetadataPath = files[i];

                try
                {
                    StoreItemMetadata metadata = await LoadItemMetadataAsync(physicalItemMetadataPath, token);
                    var phyisicalItemsBasePath = Path.GetDirectoryName(physicalItemMetadataPath);
                    if (CheckExpiration(metadata, phyisicalItemsBasePath, update: false))
                        await DeleteItemAsync(phyisicalItemsBasePath, metadata.ItemFileName, token);
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    _logger.LogWarning(ex, "Failed to remove expired bundle ({FILEPATH}) from the cache.", physicalItemMetadataPath);
                }
            }
        }

#endregion
    }
}
