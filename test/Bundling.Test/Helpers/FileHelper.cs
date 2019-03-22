using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;

namespace Karambolo.AspNetCore.Bundling.Test.Helpers
{
    public static class FileHelper
    {
        public static async Task<byte[]> GetContentAsync(IFileInfo fileInfo)
        {
            using (Stream fs = fileInfo.CreateReadStream())
            using (var ms = new MemoryStream())
            {
                await fs.CopyToAsync(ms);
                return ms.ToArray();
            }
        }
    }
}
