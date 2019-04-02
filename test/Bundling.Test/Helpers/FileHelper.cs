using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;

namespace Karambolo.AspNetCore.Bundling.Test.Helpers
{
    public static class FileHelper
    {
        public static async Task<byte[]> GetContentAsync(IFileInfo fileInfo)
        {
            using (Stream stream = fileInfo.CreateReadStream())
            using (var ms = new MemoryStream())
            {
                await stream.CopyToAsync(ms);
                return ms.ToArray();
            }
        }
    }
}
