using Microsoft.AspNetCore.Http;

namespace Karambolo.AspNetCore.Bundling
{
    public interface IBundlingContext
    {
        PathString BundlesPathPrefix { get; }
        PathString StaticFilesPathPrefix { get; }
    }
}
