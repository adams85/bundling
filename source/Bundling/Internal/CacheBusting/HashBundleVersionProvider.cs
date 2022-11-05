using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;

namespace Karambolo.AspNetCore.Bundling.Internal.CacheBusting
{
    public class HashBundleVersionProvider : IBundleVersionProvider
    {
        public void Provide(IBundleVersionProviderContext context)
        {
            byte[] hash;

#if NET5_0_OR_GREATER
            hash = SHA256.HashData(context.Content);
#else
            using (var sha256 = SHA256.Create())
                hash = sha256.ComputeHash(context.Content);
#endif

            context.Result = WebEncoders.Base64UrlEncode(hash);
        }
    }
}
