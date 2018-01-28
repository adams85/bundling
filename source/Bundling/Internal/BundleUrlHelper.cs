using Microsoft.AspNetCore.Http;

namespace Karambolo.AspNetCore.Bundling.Internal
{
    public interface IBundleUrlHelper
    {
        void AddVersion(string version, ref PathString path, ref QueryString query);
        string RemoveVersion(ref PathString path, ref QueryString query);
    }
}
