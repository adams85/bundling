using System;
using Microsoft.AspNetCore.Http;

namespace Karambolo.AspNetCore.Bundling.Internal
{
    public class BundleVersionProviderContext : IBundleVersionProviderContext
    {
        public HttpContext HttpContext { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public byte[] Content { get; set; }
        public string Result { get; set; }
    }
}
