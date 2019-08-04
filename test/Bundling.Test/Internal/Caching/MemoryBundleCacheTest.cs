using System;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace Karambolo.AspNetCore.Bundling.Internal.Caching
{
    public class MemoryBundleCacheTest : BundleCacheTest
    {
        private MemoryBundleCache _cache;
        protected override IBundleCache Cache => _cache;

        protected override bool ProvidesPhysicalFiles => false;

        protected override void Setup(TimeSpan? expirationScanFrequency)
        {
            IOptions<FileSystemBundleCacheOptions> options = Options.Create(new FileSystemBundleCacheOptions
            {
                FileProvider = new PhysicalFileProvider(Environment.CurrentDirectory)
            });

            var memoryCache = new MemoryCache(Options.Create(new MemoryCacheOptions
            {
                Clock = Clock,
                ExpirationScanFrequency = expirationScanFrequency ?? default(TimeSpan),
            }));

            _cache = new MemoryBundleCache(memoryCache,
                Options.Create(new BundleGlobalOptions
                {
                    EnableChangeDetection = true
                }));
        }
    }
}

