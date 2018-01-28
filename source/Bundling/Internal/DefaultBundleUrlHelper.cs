using System.IO;
using System.Net;
using Karambolo.AspNetCore.Bundling.Internal.Helpers;
using Microsoft.AspNetCore.Http;

namespace Karambolo.AspNetCore.Bundling.Internal
{
    public class DefaultBundleUrlHelper : IBundleUrlHelper
    {
        const string versionPrefix = ".v";

        public void AddVersion(string version, ref PathString path, ref QueryString query)
        {
            var fileName = UrlUtils.GetFileName(path, out string basePath);

            var extension = Path.GetExtension(fileName);
            fileName = Path.GetFileNameWithoutExtension(fileName);

            fileName = string.Concat(fileName, versionPrefix, WebUtility.UrlEncode(version));

            path = string.Concat(basePath, fileName, extension);
        }

        public string RemoveVersion(ref PathString path, ref QueryString query)
        {
            var fileName = UrlUtils.GetFileName(path, out string basePath);

            var extension = Path.GetExtension(fileName);
            fileName = Path.GetFileNameWithoutExtension(fileName);

            var index = fileName.LastIndexOf(versionPrefix);
            if (index < 0)
                return null;

            var result = fileName.Substring(index + versionPrefix.Length);
            fileName = fileName.Substring(0, index);

            path = string.Concat(basePath, fileName, extension);
            return result;
        }
    }
}
