using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;

namespace Karambolo.AspNetCore.Bundling.Internal
{
    public interface IStaticFileUrlHelper
    {
        string AddVersion(string url, IUrlHelper urlHelper, StaticFileUrlToFileMapper mapper);
        string AddVersion(string url, IUrlHelper urlHelper, IFileProvider fileProvider, string filePath);
    }
}
