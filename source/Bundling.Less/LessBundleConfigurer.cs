using System;
using Karambolo.AspNetCore.Bundling;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Builder
{
    public class LessBundleConfigurer : BundleConfigurer<LessBundleConfigurer>
    {
        public LessBundleConfigurer(Bundle bundle, IFileProvider sourceFileProvider, IServiceProvider appServices)
            : base(bundle, sourceFileProvider, appServices) { }
    }
}
