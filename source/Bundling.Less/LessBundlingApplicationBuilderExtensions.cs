using System;
using Karambolo.AspNetCore.Bundling;
using Karambolo.AspNetCore.Bundling.Less;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Builder
{
    public static class LessBundlingApplicationBuilderExtensions
    {
        public static LessBundleConfigurer AddLess(this BundleCollectionConfigurer configurer, PathString path)
        {
            if (configurer == null)
                throw new ArgumentNullException(nameof(configurer));

            var bundle = new Bundle(path, configurer.GetDefaults(LessBundleConfiguration.BundleType));
            configurer.Bundles.Add(bundle);
            return new LessBundleConfigurer(bundle, configurer.Bundles.SourceFileProvider, configurer.Bundles.CaseSensitiveSourceFilePaths, configurer.AppServices);
        }
    }
}
