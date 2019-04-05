using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;

namespace Karambolo.AspNetCore.Bundling
{
    public abstract class BundlingConfiguration
    {
        public static readonly PathString DefaultBundlesPathPrefix = "/bundles";

        public virtual IFileProvider SourceFileProvider { get; }
        public virtual bool? CaseSensitiveSourceFilePaths { get; }
        public virtual PathString? StaticFilesPathPrefix { get; }
        public virtual PathString? BundlesPathPrefix { get; }

        public abstract void Configure(BundleCollectionConfigurer bundles);
    }

    public abstract class DesignTimeBundlingConfiguration : BundlingConfiguration, IBundleGlobalConfiguration
    {
        public virtual PathString? AppBasePath { get; }
        public virtual string OutputBasePath { get; }

        public virtual IBundleBuilder Builder { get; }
        public virtual IReadOnlyList<IFileBundleSourceFilter> FileFilters { get; }
        public virtual IReadOnlyList<IBundleItemTransform> ItemTransforms { get; }
        public virtual IReadOnlyList<IBundleTransform> Transforms { get; }

        public virtual IEnumerable<IBundlingModule> Modules { get; } = new IBundlingModule[]
        {
            new CssBundlingModule(),
            new JsBundlingModule(),
        };
    }
}
