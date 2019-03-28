using System;
using Karambolo.AspNetCore.Bundling;
using Karambolo.AspNetCore.Bundling.Sass;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Builder
{
    public static class SassBundlingApplicationBuilderExtensions
    {
        public static SassBundleConfigurer AddSass(this BundleCollectionConfigurer configurer, PathString path)
        {
            if (configurer == null)
                throw new ArgumentNullException(nameof(configurer));

            var bundle = new Bundle(path, configurer.GetDefaults(SassBundleConfiguration.BundleType));
            configurer.Bundles.Add(bundle);
            return new SassBundleConfigurer(bundle, configurer.Bundles.SourceFileProvider, configurer.AppServices);
        }
    }
}
