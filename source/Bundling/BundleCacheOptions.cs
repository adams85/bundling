using System;
using Microsoft.Extensions.Caching.Memory;

namespace Karambolo.AspNetCore.Bundling
{
    public interface IBundleCacheOptions
    {
        bool NoCache { get; }
        DateTimeOffset? AbsoluteExpiration { get; }
        TimeSpan? SlidingExpiration { get; }
        CacheItemPriority Priority { get; }
    }

    public class BundleCacheOptions : IBundleCacheOptions
    {
        public static readonly IBundleCacheOptions Default = new BundleCacheOptions();

        public BundleCacheOptions() { }

        public BundleCacheOptions(IBundleCacheOptions other)
        {
            if (other != null)
            {
                NoCache = other.NoCache;
                AbsoluteExpiration = other.AbsoluteExpiration;
                SlidingExpiration = other.SlidingExpiration;
                Priority = other.Priority;
            }
        }

        public bool NoCache { get; set; }
        public DateTimeOffset? AbsoluteExpiration { get; set; }
        public TimeSpan? SlidingExpiration { get; set; }
        public CacheItemPriority Priority { get; set; }
    }
}
