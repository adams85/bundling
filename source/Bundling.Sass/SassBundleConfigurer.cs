using System;
using Karambolo.AspNetCore.Bundling;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Builder
{
    public class SassBundleConfigurer : BundleConfigurer<SassBundleConfigurer>
    {
        public SassBundleConfigurer(Bundle bundle, IFileProvider sourceFileProvider, IServiceProvider appServices)
            : base(bundle, sourceFileProvider, appServices) { }
    }
}
