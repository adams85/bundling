using Karambolo.AspNetCore.Bundling;
using Karambolo.AspNetCore.Bundling.Less;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Builder
{
    public static class ConfigurationExtensions
    {
        public static BundleConfigurer AddLess(this BundleCollectionConfigurer @this, PathString path)
        {
            var bundle = new Bundle(path, @this.GetDefaults(LessBundleConfiguration.BundleType));
            @this.Bundles.Add(bundle);
            return new BundleConfigurer(bundle, @this.Bundles.SourceFileProvider, @this.AppServices);
        }
    }
}
