using System;
using Karambolo.AspNetCore.Bundling;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Builder
{
    public class CssBundleConfigurer : BundleConfigurer<CssBundleConfigurer>
    {
        public CssBundleConfigurer(Bundle bundle, IFileProvider sourceFileProvider, bool caseSensitiveSourceFilePaths, IServiceProvider appServices)
            : base(bundle, sourceFileProvider, caseSensitiveSourceFilePaths, appServices) { }

        public CssBundleConfigurer EnableSourceIncludes(bool value = true)
        {
            Bundle.RenderSourceIncludes = value;
            return this;
        }
    }
}
