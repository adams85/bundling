using System;
using Karambolo.AspNetCore.Bundling.Internal;
using Microsoft.Extensions.Options;

namespace Karambolo.AspNetCore.Bundling
{
    public class BundleGlobalOptions : BundleGlobalDefaultsOptions
    {
        internal class Configurer : BundleDefaultsConfigurerBase<BundleGlobalOptions>
        {
            public Configurer(Action<BundleGlobalOptions, IServiceProvider> action, IServiceProvider serviceProvider)
                : base(action, serviceProvider) { }

            protected override string Name => Options.DefaultName;

            protected override void SetDefaults(BundleGlobalOptions options)
            {
                options.Builder = new DefaultBundleBuilder();
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
