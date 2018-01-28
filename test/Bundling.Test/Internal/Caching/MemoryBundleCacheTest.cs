using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using Xunit;

namespace Karambolo.AspNetCore.Bundling.Internal.Caching
{
    public class MemoryBundleCacheTest : BundleCacheTest
    {
        MemoryBundleCache _cache;
        protected override IBundleCache Cache => _cache;

        protected override bool ProvidesPhysicalFiles => false;

        protected override void Setup(TimeSpan? expirationScanFrequency)
        {
            var options = Options.Create(new FileSystemBundleCacheOptions
            {
                FileProvider = new PhysicalFileProvider(Environment.CurrentDirectory)
            });

            var loggerProvider = new ConsoleLoggerProvider((s, l) => l >= LogLevel.Warning, true);
            var loggerFactory = new LoggerFactory(new[] { loggerProvider });

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

