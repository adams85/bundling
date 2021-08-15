using System;
using Karambolo.AspNetCore.Bundling;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Builder
{
    public class JsBundleConfigurer : BundleConfigurer<JsBundleConfigurer>
    {
        public JsBundleConfigurer(Bundle bundle, IFileProvider sourceFileProvider, bool caseSensitiveSourceFilePaths, IServiceProvider appServices)
            : base(bundle, sourceFileProvider, caseSensitiveSourceFilePaths, appServices) { }

        public JsBundleConfigurer DisableSourceIncludes()
        {
            Bundle.RenderSourceIncludes = false;
            return this;
        }

        public JsBundleConfigurer EnableSourceIncludes()
        {
            Bundle.RenderSourceIncludes = true;
            return this;
        }
    }
}
