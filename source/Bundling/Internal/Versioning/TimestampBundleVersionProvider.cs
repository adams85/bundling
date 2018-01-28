using Microsoft.AspNetCore.Mvc.TagHelpers.Internal;
using Microsoft.AspNetCore.WebUtilities;

namespace Karambolo.AspNetCore.Bundling.Internal.Versioning
{
    public class TimestampBundleVersionProvider : IBundleVersionProvider
    {
        public void Provide(IBundleVersionProviderContext context)
        {
            var ticks = context.Timestamp.Ticks;

            var bytes = new byte[sizeof(long)];
            for (var i = 0; i < sizeof(long); i++)
                bytes[i] = (byte)(ticks >> ((sizeof(long) - 1 - i) << 3) & 0xFF);

            context.Result = WebEncoders.Base64UrlEncode(bytes);
        }
    }
}
