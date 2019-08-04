using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;

namespace Karambolo.AspNetCore.Bundling.Internal.Versioning
{
    public class HashBundleVersionProvider : IBundleVersionProvider
    {
        public void Provide(IBundleVersionProviderContext context)
        {
            byte[] hash;
            using (var sha256 = SHA256.Create())
                hash = sha256.ComputeHash(context.Content);

            context.Result = WebEncoders.Base64UrlEncode(hash);
        }
    }
}
