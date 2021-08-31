using System;
using Karambolo.AspNetCore.Bundling.Internal;
using Karambolo.AspNetCore.Bundling.Internal.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace Karambolo.AspNetCore.Bundling
{
#if !NETCOREAPP3_0_OR_GREATER
    using Microsoft.AspNetCore.Hosting;
    using IWebHostEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;
#else
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Hosting;
#endif

    public class BundleGlobalOptions : BundleGlobalDefaultsOptions
    {
        internal sealed class Configurer : BundleDefaultsConfigurerBase<BundleGlobalOptions>
        {
            public Configurer(Action<BundleGlobalOptions, IServiceProvider> action, IServiceProvider serviceProvider)
                : base(action, serviceProvider) { }

            protected override string Name => Options.DefaultName;

            protected override void SetDefaults(BundleGlobalOptions options)
            {
                IWebHostEnvironment env = ServiceProvider.GetRequiredService<IWebHostEnvironment>();

                options.EnableCacheBusting = !env.IsDevelopment();

                options.Builder = new DefaultBundleBuilder();

                options.SourceItemToUrlMapper = (item, bundlingContext, urlHelper) =>
                    item.ItemTransformContext is IFileBundleItemTransformContext fileItemContext &&
                        new AbstractionFile.FileProviderEqualityComparer(fileItemContext.CaseSensitiveFilePaths).Equals(fileItemContext.FileProvider, env.WebRootFileProvider) ?
                    urlHelper.Content("~" + bundlingContext.StaticFilesPathPrefix.Add(new PathString(UrlUtils.NormalizePath(fileItemContext.FilePath)))) :
                    null;

                options.StaticFileUrlToFileMapper = delegate (string url, IUrlHelper urlHelper, out IFileProvider fileProvider, out string filePath, out bool caseSensitiveFilePaths)
                {
                    if (urlHelper.IsLocalUrl(url))
                    {
                        fileProvider = env.WebRootFileProvider;
                        filePath = PathString.FromUriComponent(UrlUtils.NormalizePath(urlHelper.Content(url))).Value;
                        caseSensitiveFilePaths = AbstractionFile.GetDefaultCaseSensitiveFilePaths(fileProvider);
                        return true;
                    }

                    (fileProvider, filePath, caseSensitiveFilePaths) = (default, default, default);
                    return false;
                };
            }
        }

        public bool EnableMinification { get; set; }
        public bool EnableChangeDetection { get; set; }
        public bool EnableCacheBusting { get; set; }
        public bool EnableCacheHeader { get; set; }
        public TimeSpan? CacheHeaderMaxAge { get; set; }

        public StaticFileUrlToFileMapper StaticFileUrlToFileMapper { get; set; }

        internal void Merge(IBundleGlobalConfiguration configuration)
        {
            Builder = configuration.Builder ?? Builder;
            FileFilters = configuration.FileFilters ?? FileFilters;
            ItemTransforms = configuration.ItemTransforms ?? ItemTransforms;
            Transforms = configuration.Transforms ?? Transforms;
        }
    }
}
