using System;
using System.IO;
using System.Net;
using Karambolo.AspNetCore.Bundling.Internal.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Karambolo.AspNetCore.Bundling.Internal.CacheBusting
{
    public class FileNameVersioningBundleUrlHelper : IBundleUrlHelper
    {
        private const string VersionPrefix = ".v";

        private static void DeconstructFileName(StringSegment fileNameSegment, out ReadOnlySpan<char> fileName, out ReadOnlySpan<char> extension)
        {
#if NETCOREAPP3_0_OR_GREATER
            fileName = fileNameSegment;
            extension = Path.GetExtension(fileName);
            fileName = Path.GetFileNameWithoutExtension(fileName);
#else
            string fileNameString = fileNameSegment.ToString();
            extension = Path.GetExtension(fileNameString).AsSpan();
            fileName = Path.GetFileNameWithoutExtension(fileNameString).AsSpan();
#endif
        }

        public void AddVersion(string version, ref PathString path, ref QueryString query)
        {
            DeconstructFileName(UrlUtils.GetFileNameSegment(path.ToString(), out StringSegment basePathSegment), out ReadOnlySpan<char> fileName, out ReadOnlySpan<char> extension);

            fileName = fileName.Concat(VersionPrefix.AsSpan(), WebUtility.UrlEncode(version).AsSpan()).ToString().AsSpan();

            path = basePathSegment.AsSpan().Concat(fileName, extension);
        }


        public string RemoveVersion(ref PathString path, ref QueryString query)
        {
            DeconstructFileName(UrlUtils.GetFileNameSegment(path.ToString(), out StringSegment basePathSegment), out ReadOnlySpan<char> fileName, out ReadOnlySpan<char> extension);

            var index = fileName.LastIndexOf(VersionPrefix.AsSpan());
            if (index < 0)
                return null;

            path = basePathSegment.AsSpan().Concat(fileName.Slice(0, index), extension);
            return fileName.Slice(index + VersionPrefix.Length).ToString();
        }
    }
}
