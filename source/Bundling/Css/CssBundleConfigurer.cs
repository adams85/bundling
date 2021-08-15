using System;
using Karambolo.AspNetCore.Bundling;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Builder
{
    public class CssBundleConfigurer : BundleConfigurer<CssBundleConfigurer>
    {
        public CssBundleConfigurer(Bundle bundle, IFileProvider sourceFileProvider, bool caseSensitiveSourceFilePaths, IServiceProvider appServices)
            : base(bundle, sourceFileProvider, caseSensitiveSourceFilePaths, appServices) { }

        public CssBundleConfigurer DisableSourceIncludes()
        {
            Bundle.RenderSourceIncludes = false;
            return this;
        }

        public CssBundleConfigurer EnableSourceIncludes()
        {
            Bundle.RenderSourceIncludes = true;
            return this;
        }
    }
}
