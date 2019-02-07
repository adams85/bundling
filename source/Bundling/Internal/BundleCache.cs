using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;

namespace Karambolo.AspNetCore.Bundling.Internal
{
    public readonly struct BundleCacheKey : IEquatable<BundleCacheKey>
    {
        public BundleCacheKey(int managerId, PathString path, QueryString query)
        {
            ManagerId = managerId;
            Path = path;
            Query = query;
        }

        public int ManagerId { get; }
        public PathString Path { get; }
        public QueryString Query { get; }

        public bool Equals(BundleCacheKey other)
        {
            return ManagerId == other.ManagerId && Path == other.Path && Query == other.Query;
        }

        public override bool Equals(object obj)
        {
            return obj is BundleCacheKey other ? Equals(other) : false;
        }

        public override int GetHashCode()
        {
            return ManagerId.GetHashCode() ^ Path.GetHashCode() ^ Query.GetHashCode();
        }
    }

    public class BundleCacheData
    {
        public DateTimeOffset Timestamp { get; set; }
        public string Version { get; set; }
        public byte[] Content { get; set; }
    }

    public interface IBundleCacheItem
    {
        IFileInfo FileInfo { get; }
        DateTimeOffset Timestamp { get; }
        string Version { get; }
        IDisposable FileReleaser { get; }
    }

    public interface IBundleCache
    {
        Task<IBundleCacheItem> GetOrAddAsync(BundleCacheKey key, Func<CancellationToken, Task<BundleCacheData>> factory, CancellationToken token,
            IBundleCacheOptions cacheOptions, bool lockFile = false);
        Task RemoveAsync(BundleCacheKey key, CancellationToken token);
        Task RemoveAllAsync(int managerId, PathString bundlePath, CancellationToken token);
    }
}
