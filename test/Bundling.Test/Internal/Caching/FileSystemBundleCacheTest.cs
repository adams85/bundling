using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using Xunit;

namespace Karambolo.AspNetCore.Bundling.Internal.Caching
{
    public class FileSystemBundleCacheTest : BundleCacheTest
    {
        static int counter;

        string _basePath;

        FileSystemBundleCache _cache;
        protected override IBundleCache Cache => _cache;

        protected override bool ProvidesPhysicalFiles => true;

        void Cleanup(string basePath)
        {
            _cache.Reset();
        }

        protected override void Setup(TimeSpan? expirationScanFrequency)
        {
            var loggerProvider = new ConsoleLoggerProvider((s, l) => l >= LogLevel.Warning, true);
            var loggerFactory = new LoggerFactory(new[] { loggerProvider });

            var cache = new FileSystemBundleCache(CancellationToken.None, null, loggerFactory, Clock,
                Options.Create(new FileSystemBundleCacheOptions
                {
                    FileProvider = new PhysicalFileProvider(Environment.CurrentDirectory),
                    EnableExpirationScanning = expirationScanFrequency.HasValue,
                    ExpirationScanFrequency = expirationScanFrequency ?? default(TimeSpan),
                    AutoResetOnCreate = true,
                    BasePath = "Cache" + Interlocked.Increment(ref counter).ToString()
                }), 
                Options.Create(new BundleGlobalOptions
                {
                    EnableChangeDetection = true                    
                }));

            _cache = cache;
            _basePath = cache.PhysicalBasePath;
        }

        protected override void Teardown()
        {
            _cache.Reset();
        }
    }
}

