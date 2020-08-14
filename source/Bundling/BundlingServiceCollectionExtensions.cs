using System;
using System.Collections.Generic;
using Karambolo.AspNetCore.Bundling;
using Karambolo.AspNetCore.Bundling.Css;
using Karambolo.AspNetCore.Bundling.Internal;
using Karambolo.AspNetCore.Bundling.Internal.Caching;
using Karambolo.AspNetCore.Bundling.Internal.Configuration;
using Karambolo.AspNetCore.Bundling.Internal.Helpers;
using Karambolo.AspNetCore.Bundling.Internal.Versioning;
using Karambolo.AspNetCore.Bundling.Js;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
#if NETSTANDARD2_0
    using Microsoft.AspNetCore.Hosting;
    using IWebHostEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;
    using IHostApplicationLifetime = Microsoft.AspNetCore.Hosting.IApplicationLifetime;
#else
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Hosting;
#endif

    public static class BundlingServiceCollectionExtensions
    {
        internal static BundlingConfigurer AddBundlingCore(this IServiceCollection services, Action<BundleGlobalOptions, IServiceProvider> configure)
        {
            services.AddSingleton<IConfigureOptions<BundleGlobalOptions>>(sp => new BundleGlobalOptions.Configurer(configure, sp));

            services.TryAddSingleton<ISystemClock, SystemClock>();

            services.TryAddSingleton<IConfigFileManager, ConfigFileManager>();

            services.AddSingleton<IBundleModelFactory, DefaultBundleModelFactory>();
            services.AddSingleton(sp => new Lazy<IEnumerable<IBundleModelFactory>>(() => sp.GetRequiredService<IEnumerable<IBundleModelFactory>>()));

            services.TryAddSingleton<ICssMinifier, NullCssMinifier>();
            services.TryAddSingleton<IJsMinifier, NullJsMinifier>();

            return new BundlingConfigurer(services);
        }

        public static BundlingConfigurer AddBundling(this IServiceCollection services, Action<BundleGlobalOptions, IServiceProvider> configure = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            services.AddOptions().AddLogging();

            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            services.TryAddSingleton<IBundleManagerFactory, BundleManagerFactory>();
            services.TryAddSingleton<IBundleVersionProvider>(NullBundleVersionProvider.Instance);
            services.TryAddSingleton<IBundleUrlHelper, DefaultBundleUrlHelper>();

            return services.AddBundlingCore(configure);
        }

        public static BundlingConfigurer UseMemoryCaching(this BundlingConfigurer configurer)
        {
            if (configurer == null)
                throw new ArgumentNullException(nameof(configurer));

            configurer.Services.AddMemoryCache();
            configurer.Services.Replace(ServiceDescriptor.Singleton<IBundleCache, MemoryBundleCache>());

            return configurer;
        }

        public static BundlingConfigurer UseFileSystemCaching(this BundlingConfigurer configurer, Action<FileSystemBundleCacheOptions> configure = null)
        {
            if (configurer == null)
                throw new ArgumentNullException(nameof(configurer));

            configurer.Services.Replace(ServiceDescriptor.Singleton<IBundleCache>(sp => new FileSystemBundleCache(
                sp.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping, sp.GetRequiredService<IWebHostEnvironment>(), sp.GetRequiredService<ILoggerFactory>(),
                sp.GetRequiredService<ISystemClock>(), sp.GetRequiredService<IOptions<FileSystemBundleCacheOptions>>())));

            if (configure != null)
                configurer.Services.Configure(configure);

            return configurer;
        }

        public static BundlingConfigurer UseHashVersioning(this BundlingConfigurer configurer)
        {
            if (configurer == null)
                throw new ArgumentNullException(nameof(configurer));

            configurer.Services.Replace(ServiceDescriptor.Singleton<IBundleVersionProvider, HashBundleVersionProvider>());

            return configurer;
        }

        public static BundlingConfigurer UseTimestampVersioning(this BundlingConfigurer configurer)
        {
            if (configurer == null)
                throw new ArgumentNullException(nameof(configurer));

            configurer.Services.Replace(ServiceDescriptor.Singleton<IBundleVersionProvider, TimestampBundleVersionProvider>());

            return configurer;
        }

        public static BundlingConfigurer EnableMinification(this BundlingConfigurer configurer)
        {
            if (configurer == null)
                throw new ArgumentNullException(nameof(configurer));

            configurer.Services.Configure<BundleGlobalOptions>(o => o.EnableMinification = true);

            return configurer;
        }

        public static BundlingConfigurer EnableChangeDetection(this BundlingConfigurer configurer)
        {
            if (configurer == null)
                throw new ArgumentNullException(nameof(configurer));

            configurer.Services.Configure<BundleGlobalOptions>(o => o.EnableChangeDetection = true);

            return configurer;
        }

        public static BundlingConfigurer EnableCacheHeader(this BundlingConfigurer configurer, TimeSpan? maxAge = null)
        {
            if (configurer == null)
                throw new ArgumentNullException(nameof(configurer));

            configurer.Services.Configure<BundleGlobalOptions>(o =>
            {
                o.EnableCacheHeader = true;
                o.CacheHeaderMaxAge = maxAge;
            });

            return configurer;
        }

        public static BundlingConfigurer AddCss(this BundlingConfigurer configurer, Action<BundleDefaultsOptions, IServiceProvider> configure = null)
        {
            if (configurer == null)
                throw new ArgumentNullException(nameof(configurer));

            configurer.Services.AddSingleton<IConfigureOptions<BundleDefaultsOptions>>(sp => new CssBundleConfiguration.Configurer(configure, sp));
            configurer.Services.AddSingleton<IConfigurationHelper, CssBundleConfiguration.Helper>();
            configurer.Services.AddSingleton<IExtensionMapper, CssBundleConfiguration.ExtensionMapper>();

            return configurer;
        }

        public static BundlingConfigurer AddJs(this BundlingConfigurer configurer, Action<BundleDefaultsOptions, IServiceProvider> configure = null)
        {
            if (configurer == null)
                throw new ArgumentNullException(nameof(configurer));

            configurer.Services.AddSingleton<IConfigureOptions<BundleDefaultsOptions>>(sp => new JsBundleConfiguration.Configurer(configure, sp));
            configurer.Services.AddSingleton<IConfigurationHelper, JsBundleConfiguration.Helper>();
            configurer.Services.AddSingleton<IExtensionMapper, JsBundleConfiguration.ExtensionMapper>();

            return configurer;
        }

        public static BundlingConfigurer UseDefaults(this BundlingConfigurer configurer, IWebHostEnvironment environment)
        {
            if (configurer == null)
                throw new ArgumentNullException(nameof(configurer));

            if (environment == null)
                throw new ArgumentNullException(nameof(environment));

            configurer
                .AddCss()
                .AddJs()
                .UseHashVersioning()
                .UseMemoryCaching();

            if (!environment.IsDevelopment())
                configurer.EnableMinification();
            else
                configurer.EnableChangeDetection();

            return configurer;
        }
    }
}
