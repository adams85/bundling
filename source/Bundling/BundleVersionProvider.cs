using System;
using Microsoft.AspNetCore.Http;

namespace Karambolo.AspNetCore.Bundling
{
    public interface IBundleVersionProviderContext
    {
        HttpContext HttpContext { get; }
        DateTimeOffset Timestamp { get; }
        byte[] Content { get; }
        string Result { get; set; }
    }

    public interface IBundleVersionProvider
    {
        void Provide(IBundleVersionProviderContext context);
    }
}
