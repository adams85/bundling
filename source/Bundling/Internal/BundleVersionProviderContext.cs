using System;
using System.Threading;

namespace Karambolo.AspNetCore.Bundling.Internal
{
    public class BundleVersionProviderContext : IBundleVersionProviderContext
    {
        public DateTimeOffset Timestamp { get; set; }
        public byte[] Content { get; set; }
        public CancellationToken CancellationToken { get; set; }
        public string Result { get; set; }
    }
}
