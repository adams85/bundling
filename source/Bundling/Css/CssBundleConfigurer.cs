using System;
using Karambolo.AspNetCore.Bundling;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Builder
{
    public class CssBundleConfigurer : BundleConfigurer<CssBundleConfigurer>
    {
        public CssBundleConfigurer(Bundle bundle, IFileProvider sourceFileProvider, IServiceProvider appServices)
            : base(bundle, sourceFileProvider, appServices) { }
    }
}
