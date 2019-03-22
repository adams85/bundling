using Microsoft.AspNetCore.Mvc.TagHelpers.Internal;
using Microsoft.AspNetCore.WebUtilities;

namespace Karambolo.AspNetCore.Bundling.Internal.Versioning
{
    public class HashBundleVersionProvider : IBundleVersionProvider
    {
        public void Provide(IBundleVersionProviderContext context)
        {
            byte[] hash;
            using (System.Security.Cryptography.SHA256 sha256 = CryptographyAlgorithms.CreateSHA256())
                hash = sha256.ComputeHash(context.Content);

            context.Result = WebEncoders.Base64UrlEncode(hash);
        }
    }
}
