using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Karambolo.AspNetCore.Bundling.Internal
{
    public class BundleBuilderContext : IBundleBuilderContext
    {
        public IBundlingContext BundlingContext { get; set; }
        public HttpContext HttpContext { get; set; }
        public IDictionary<string, StringValues> Params { get; set; }
        public IBundleModel Bundle { get; set; }
        public CancellationToken CancellationToken => HttpContext.RequestAborted;

        public string Result { get; set; }
    }
}
