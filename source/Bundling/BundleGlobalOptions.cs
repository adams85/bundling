using System;
using Karambolo.AspNetCore.Bundling.Internal;
using Karambolo.AspNetCore.Bundling.Internal.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Karambolo.AspNetCore.Bundling
{
#if NETSTANDARD2_0
    using IWebHostEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;
#else
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.FileProviders;
#endif

    public class BundleGlobalOptions : BundleGlobalDefaultsOptions
    {
        internal class Configurer : BundleDefaultsConfigurerBase<BundleGlobalOptions>
        {
            private static string DefaultResolveSourceItemUrl(IBundleSourceBuildItem item, IBundlingContext bundlingContext, IUrlHelper urlHelper, IWebHostEnvironment env)
            {
                return
                    item.ItemTransformContext is IFileBundleItemTransformContext fileItemContext &&
                        new AbstractionFile.FileProviderEqualityComparer(fileItemContext.CaseSensitiveFilePaths).Equals(fileItemContext.FileProvider, env.WebRootFileProvider) ?
                    urlHelper.Content("~" + bundlingContext.StaticFilesPathPrefix.Add(UrlUtils.NormalizePath(fileItemContext.FilePath)).ToString()) :
                    null;
            }

            public Configurer(Action<BundleGlobalOptions, IServiceProvider> action, IServiceProvider serviceProvider)
                : base(action, serviceProvider) { }

            protected override string Name => Options.DefaultName;

            protected override void SetDefaults(BundleGlobalOptions options)
            {
                IWebHostEnvironment env = ServiceProvider.GetRequiredService<IWebHostEnvironment>();

                options.Builder = new DefaultBundleBuilder();
                options.SourceItemUrlResolver = (item, bundlingContext, urlHelper) => DefaultResolveSourceItemUrl(item, bundlingContext, urlHelper, env);
            }
        }

        public bool EnableMinification { get; set; }
        public bool EnableChangeDetection { get; set; }
        public bool EnableCacheHeader { get; set; }
        public TimeSpan? CacheHeaderMaxAge { get; set; }

        internal void Merge(IBundleGlobalConfiguration configuration)
        {
            Builder = configuration.Builder ?? Builder;
            FileFilters = configuration.FileFilters ?? FileFilters;
            ItemTransforms = configuration.ItemTransforms ?? ItemTransforms;
            Transforms = configuration.Transforms ?? Transforms;
        }
    }
}
