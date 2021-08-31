using System;
using Karambolo.AspNetCore.Bundling;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Builder
{
    public class JsBundleConfigurer : BundleConfigurer<JsBundleConfigurer>
    {
        public JsBundleConfigurer(Bundle bundle, IFileProvider sourceFileProvider, bool caseSensitiveSourceFilePaths, IServiceProvider appServices)
            : base(bundle, sourceFileProvider, caseSensitiveSourceFilePaths, appServices) { }

        public JsBundleConfigurer EnableSourceIncludes(bool value = true)
        {
            Bundle.RenderSourceIncludes = value;
            return this;
        }
    }
}
