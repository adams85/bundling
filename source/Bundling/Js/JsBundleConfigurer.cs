using System;
using Karambolo.AspNetCore.Bundling;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Builder
{
    public class JsBundleConfigurer : BundleConfigurer<JsBundleConfigurer>
    {
        public JsBundleConfigurer(Bundle bundle, IFileProvider sourceFileProvider, IServiceProvider appServices)
            : base(bundle, sourceFileProvider, appServices) { }
    }
}
