using System;
using System.Threading;

namespace Karambolo.AspNetCore.Bundling
{
    public interface IBundleVersionProviderContext
    {
        DateTimeOffset Timestamp { get; }
        byte[] Content { get; }
        CancellationToken CancellationToken { get; set; }
        string Result { get; set; }
    }

    public interface IBundleVersionProvider
    {
        void Provide(IBundleVersionProviderContext context);
    }
}
