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
    public static class BundlingApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseBundling(this IApplicationBuilder builder, Action<BundleCollectionConfigurer> configureBundles = null)
        {
            return builder.UseBundling(BundlingOptions.Default, configureBundles);
        }

        public static IApplicationBuilder UseBundling(this IApplicationBuilder builder, BundlingOptions options, Action<BundleCollectionConfigurer> configureBundles = null)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            if (options == null)
                throw new ArgumentNullException(nameof(options));

            if (HttpContextStatic.Current == null)
                HttpContextStatic.Initialize(builder.ApplicationServices.GetRequiredService<IHttpContextAccessor>());

            IFileProvider sourceFileProvider =
                options.SourceFileProvider ??
                builder.ApplicationServices.GetRequiredService<IHostingEnvironment>().WebRootFileProvider;

            var bundles = new BundleCollection(options.RequestPath, sourceFileProvider);

            configureBundles?.Invoke(new BundleCollectionConfigurer(bundles, builder.ApplicationServices));

            builder.UseMiddleware<BundlingMiddleware>(bundles, Options.Create(options));

            return builder;
        }

        public static BundleCollectionConfigurer LoadFromConfigFile(this BundleCollectionConfigurer configurer, TextReader reader,
            ConfigFilePathMapper pathMapper = null)
        {
            if (configurer == null)
                throw new ArgumentNullException(nameof(configurer));

            IConfigFileManager configFileManager = configurer.AppServices.GetRequiredService<IConfigFileManager>();
            configFileManager.Load(configurer.Bundles, reader, pathMapper);

            return configurer;
        }

        public static BundleCollectionConfigurer LoadFromConfigFile(this BundleCollectionConfigurer configurer, IFileInfo fileInfo,
            ConfigFilePathMapper pathMapper = null)
        {
            if (configurer == null)
                throw new ArgumentNullException(nameof(configurer));

            if (fileInfo == null)
                throw new ArgumentNullException(nameof(fileInfo));

            using (Stream stream = fileInfo.CreateReadStream())
            using (var reader = new StreamReader(stream))
                return configurer.LoadFromConfigFile(reader, pathMapper);
        }

        public static BundleCollectionConfigurer LoadFromConfigFile(this BundleCollectionConfigurer configurer, string path, IFileProvider fileProvider,
            ConfigFilePathMapper pathMapper = null)
        {
            if (configurer == null)
                throw new ArgumentNullException(nameof(configurer));

            if (path == null)
                throw new ArgumentNullException(nameof(path));

            if (fileProvider == null)
                throw new ArgumentNullException(nameof(fileProvider));

            return configurer.LoadFromConfigFile(fileProvider.GetFileInfo(path), pathMapper);
        }

        public static CssBundleConfigurer AddCss(this BundleCollectionConfigurer configurer, PathString path)
        {
            if (configurer == null)
                throw new ArgumentNullException(nameof(configurer));

            var bundle = new Bundle(path, configurer.GetDefaults(CssBundleConfiguration.BundleType));
            configurer.Bundles.Add(bundle);
            return new CssBundleConfigurer(bundle, configurer.Bundles.SourceFileProvider, configurer.AppServices);
        }

        public static JsBundleConfigurer AddJs(this BundleCollectionConfigurer configurer, PathString path)
        {
            if (configurer == null)
                throw new ArgumentNullException(nameof(configurer));

            var bundle = new Bundle(path, configurer.GetDefaults(JsBundleConfiguration.BundleType));
            configurer.Bundles.Add(bundle);
            return new JsBundleConfigurer(bundle, configurer.Bundles.SourceFileProvider, configurer.AppServices);
        }
    }
}
