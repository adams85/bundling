using System;

namespace Karambolo.AspNetCore.Bundling.Sass.Internal.Helpers
{
    internal static class SassErrorHelper
    {
        public static InvalidOperationException CompilationContextNotAccessible()
        {
            return new InvalidOperationException("No ambient compilation context is accessible currently.");
        }
    }
}
