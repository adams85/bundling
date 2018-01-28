using Microsoft.AspNetCore.Http;

namespace Karambolo.AspNetCore.Bundling.Internal
{
    public class BundlingContext : IBundlingContext
    {
        public PathString BundlesPathPrefix { get; set; }
        public PathString StaticFilesPathPrefix { get; set; }
    }
}
