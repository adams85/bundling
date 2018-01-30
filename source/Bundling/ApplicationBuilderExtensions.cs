using System;
using System.IO;
using Karambolo.AspNetCore.Bundling;
using Karambolo.AspNetCore.Bundling.Css;
using Karambolo.AspNetCore.Bundling.Internal.Configuration;
using Karambolo.AspNetCore.Bundling.Internal.Helpers;
using Karambolo.AspNetCore.Bundling.Js;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder
{
    public static class ConfigurationExtensions
    {
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
