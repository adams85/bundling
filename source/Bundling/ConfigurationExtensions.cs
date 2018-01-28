using System;
using System.Collections.Generic;
using System.IO;
using Karambolo.AspNetCore.Bundling.Css;
using Karambolo.AspNetCore.Bundling.Internal;
using Karambolo.AspNetCore.Bundling.Internal.Caching;
using Karambolo.AspNetCore.Bundling.Internal.Configuration;
using Karambolo.AspNetCore.Bundling.Internal.Helpers;
using Karambolo.AspNetCore.Bundling.Internal.Versioning;
using Karambolo.AspNetCore.Bundling.Js;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Karambolo.AspNetCore.Bundling
{
    public class BundlingConfigurer
    {
        public BundlingConfigurer(IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            Services = services;
        }

        public IServiceCollection Services { get; }
    }

    public static class ConfigurationExtensions
    {
        public static BundlingConfigurer AddBundling(this IServiceCollection @this, Action<BundleGlobalOptions, IServiceProvider> configure = null)
        {
            @this.AddOptions().AddLogging();

            @this.AddSingleton<IConfigureOptions<BundleGlobalOptions>>(sp => new BundleGlobalOptions.Configurer(configure, sp));
            @this.TryAddScoped<IScopedDisposer, DefaultScopedDisposer>();

            @this.TryAddSingleton<ISystemClock, SystemClock>();
            @this.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            @this.TryAddSingleton<IConfigFileManager, ConfigFileManager>();

            @this.AddSingleton<IBundleModelFactory, DefaultBundleModelFactory>();
            @this.AddSingleton(sp => new Lazy<IEnumerable<IBundleModelFactory>>(() => sp.GetRequiredService<IEnumerable<IBundleModelFactory>>()));

            @this.TryAddSingleton<IBundleManagerFactory, BundleManagerFactory>();
            @this.TryAddSingleton<IBundleVersionProvider>(NullBundleVersionProvider.Instance);
            @this.TryAddSingleton<IBundleUrlHelper, DefaultBundleUrlHelper>();

            return new BundlingConfigurer(@this);
        }

        public static BundlingConfigurer UseMemoryCaching(this BundlingConfigurer @this)
        {
            @this.Services.AddMemoryCache();
            @this.Services.Replace(ServiceDescriptor.Singleton<IBundleCache, MemoryBundleCache>());

            return @this;
        }

        public static BundlingConfigurer UseFileSystemCaching(this BundlingConfigurer @this, Action<FileSystemBundleCacheOptions> configure = null)
        {
            @this.Services.Replace(ServiceDescriptor.Singleton<IBundleCache>(sp => new FileSystemBundleCache(
                sp.GetRequiredService<IApplicationLifetime>().ApplicationStopping, sp.GetRequiredService<IHostingEnvironment>(), sp.GetRequiredService<ILoggerFactory>(),
                sp.GetRequiredService<ISystemClock>(), sp.GetRequiredService<IOptions<FileSystemBundleCacheOptions>>(), sp.GetRequiredService<IOptions<BundleGlobalOptions>>())));

            if (configure != null)
                @this.Services.Configure(configure);

            return @this;
        }

        public static BundlingConfigurer UseHashVersioning(this BundlingConfigurer @this)
        {
            @this.Services.Replace(ServiceDescriptor.Singleton<IBundleVersionProvider, HashBundleVersionProvider>());

            return @this;
        }

        public static BundlingConfigurer UseTimestampVersioning(this BundlingConfigurer @this)
        {
            @this.Services.Replace(ServiceDescriptor.Singleton<IBundleVersionProvider, TimestampBundleVersionProvider>());

            return @this;
        }

        public static BundlingConfigurer EnableMinification(this BundlingConfigurer @this)
        {
            @this.Services.Configure<BundleGlobalOptions>(o => o.EnableMinification = true);

            return @this;
        }

        public static BundlingConfigurer EnableChangeDetection(this BundlingConfigurer @this)
        {
            @this.Services.Configure<BundleGlobalOptions>(o => o.EnableChangeDetection = true);

            return @this;
        }

        public static BundlingConfigurer EnableCacheHeader(this BundlingConfigurer @this, TimeSpan? maxAge = null)
        {
            @this.Services.Configure<BundleGlobalOptions>(o =>
            {
                o.EnableCacheHeader = true;
                o.CacheHeaderMaxAge = maxAge;
            });

            return @this;
        }

        public static BundlingConfigurer AddCss(this BundlingConfigurer @this, Action<BundleDefaultsOptions, IServiceProvider> configure = null)
        {
            @this.Services.AddSingleton<IConfigureOptions<BundleDefaultsOptions>>(sp => new CssBundleConfiguration.Configurer(configure, sp));
            @this.Services.AddSingleton<IConfigurationHelper, CssBundleConfiguration.Helper>();
            @this.Services.AddSingleton<IExtensionMapper, CssBundleConfiguration.ExtensionMapper>();

            return @this;
        }

        public static BundlingConfigurer AddJs(this BundlingConfigurer @this, Action<BundleDefaultsOptions, IServiceProvider> configure = null)
        {
            @this.Services.AddSingleton<IConfigureOptions<BundleDefaultsOptions>>(sp => new JsBundleConfiguration.Configurer(configure, sp));
            @this.Services.AddSingleton<IConfigurationHelper, JsBundleConfiguration.Helper>();
            @this.Services.AddSingleton<IExtensionMapper, JsBundleConfiguration.ExtensionMapper>();

            return @this;
        }

        public static BundlingConfigurer UseDefaults(this BundlingConfigurer @this, IHostingEnvironment environment)
        {
            if (environment == null)
                throw new ArgumentNullException(nameof(environment));

            @this
                .AddCss()
                .AddJs()
                .UseHashVersioning()
                .UseMemoryCaching();

            if (!environment.IsDevelopment())
                @this.EnableMinification();
            else
                @this.EnableChangeDetection();

            return @this;
        }

        public static IApplicationBuilder UseBundling(this IApplicationBuilder @this, Action<BundleCollectionConfigurer> configureBundles = null)
        {
            return @this.UseBundling(BundlingOptions.Default, configureBundles);
        }

        public static IApplicationBuilder UseBundling(this IApplicationBuilder @this, BundlingOptions options, Action<BundleCollectionConfigurer> configureBundles = null)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            if (HttpContextStatic.Current == null)
                HttpContextStatic.Initialize(@this.ApplicationServices.GetRequiredService<IHttpContextAccessor>());

            var sourceFileProvider =
                options.SourceFileProvider ??
                @this.ApplicationServices.GetRequiredService<IHostingEnvironment>().WebRootFileProvider;

            var bundles = new BundleCollection(options.RequestPath, sourceFileProvider);

            configureBundles?.Invoke(new BundleCollectionConfigurer(bundles, @this.ApplicationServices));

            @this.UseMiddleware<BundlingMiddleware>(bundles, Options.Create(options));

            return @this;
        }

        public static BundleCollectionConfigurer LoadFromConfigFile(this BundleCollectionConfigurer @this, TextReader reader,
            ConfigFilePathMapper pathMapper = null)
        {
            var configFileManager = @this.AppServices.GetRequiredService<IConfigFileManager>();
            configFileManager.Load(@this.Bundles, reader, pathMapper);

            return @this;
        }

        public static BundleCollectionConfigurer LoadFromConfigFile(this BundleCollectionConfigurer @this, IFileInfo fileInfo,
            ConfigFilePathMapper pathMapper = null)
        {
            if (fileInfo == null)
                throw new ArgumentNullException(nameof(fileInfo));

            using (var stream = fileInfo.CreateReadStream())
            using (var reader = new StreamReader(stream))
                return @this.LoadFromConfigFile(reader, pathMapper);
        }

        public static BundleCollectionConfigurer LoadFromConfigFile(this BundleCollectionConfigurer @this, string path, IFileProvider fileProvider,
            ConfigFilePathMapper pathMapper = null)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            if (fileProvider == null)
                throw new ArgumentNullException(nameof(fileProvider));

            return @this.LoadFromConfigFile(fileProvider.GetFileInfo(path), pathMapper);
        }

        public static BundleConfigurer AddCss(this BundleCollectionConfigurer @this, PathString path)
        {
            var bundle = new Bundle(path, @this.GetDefaults(CssBundleConfiguration.BundleType));
            @this.Bundles.Add(bundle);
            return new BundleConfigurer(bundle, @this.Bundles.SourceFileProvider, @this.AppServices);
        }

        public static BundleConfigurer AddJs(this BundleCollectionConfigurer @this, PathString path)
        {
            var bundle = new Bundle(path, @this.GetDefaults(JsBundleConfiguration.BundleType));
            @this.Bundles.Add(bundle);
            return new BundleConfigurer(bundle, @this.Bundles.SourceFileProvider, @this.AppServices);
        }
    }
}
