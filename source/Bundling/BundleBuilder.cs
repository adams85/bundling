using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Karambolo.AspNetCore.Bundling.Internal;
using Karambolo.AspNetCore.Bundling.Internal.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Karambolo.AspNetCore.Bundling
{
    public interface IBundleBuildContext
    {
        IBundlingContext BundlingContext { get; }
        HttpContext HttpContext { get; }
        IDictionary<string, StringValues> Params { get; }
        IBundleModel Bundle { get; }
        CancellationToken CancellationToken { get; }
    }

    public interface IBundleBuilderContext : IBundleBuildContext
    {
        string Result { get; set; }
    }

    public interface IBundleBuilder
    {
        Task BuildAsync(IBundleBuilderContext context);
    }
}
