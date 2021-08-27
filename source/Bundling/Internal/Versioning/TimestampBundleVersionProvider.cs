using System;
using Microsoft.AspNetCore.WebUtilities;

namespace Karambolo.AspNetCore.Bundling.Internal.Versioning
{
    public class TimestampBundleVersionProvider : IBundleVersionProvider
    {
        public void Provide(IBundleVersionProviderContext context)
        {
            var ticks = context.Timestamp.Ticks;

#if !NETCOREAPP3_0_OR_GREATER
            var bytes = new byte[sizeof(long)];
#else
            Span<byte> bytes = stackalloc byte[sizeof(long)];
#endif

            for (var i = 0; i < sizeof(long); i++)
                bytes[i] = (byte)(ticks >> ((sizeof(long) - 1 - i) << 3) & 0xFF);

            context.Result = WebEncoders.Base64UrlEncode(bytes);
        }
    }
}
