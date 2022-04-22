using System;
using System.IO;
using System.Security.Cryptography;
using Karambolo.AspNetCore.Bundling.Internal.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Karambolo.AspNetCore.Bundling.Internal.CacheBusting
{
    // based on: https://github.com/dotnet/aspnetcore/blob/v3.1.18/src/Mvc/Mvc.Razor/src/Infrastructure/DefaultFileVersionProvider.cs
    public class DefaultStaticFileUrlHelper : IStaticFileUrlHelper
    {
        private static string GetHashForFile(IFileInfo fileInfo)
        {
            using (var sha256 = SHA256.Create())
            {
                using (Stream readStream = fileInfo.CreateReadStream())
                {
                    var hash = sha256.ComputeHash(readStream);
                    return WebEncoders.Base64UrlEncode(hash);
                }
            }
        }

        private readonly IMemoryCache _cache;

        public DefaultStaticFileUrlHelper()
        {
            _cache = new MemoryCache(new MemoryCacheOptions
            {
                SizeLimit = 1024 * 1024 // 1MB
            });
        }

#if NETCOREAPP3_0_OR_GREATER
        public DefaultStaticFileUrlHelper(Microsoft.AspNetCore.Mvc.Razor.Infrastructure.TagHelperMemoryCacheProvider cacheProvider)
        {
            _cache = cacheProvider.Cache;
        }
#endif

        protected virtual string GetVersion<TState>(string url, IUrlHelper urlHelper, TState state, Func<TState, IUrlHelper, (IFileProvider, string)> getFileInfo)
        {
            if (_cache.TryGetValue(url, out string version))
                return version;

            var cacheEntryOptions = new MemoryCacheEntryOptions();

            (IFileProvider fileProvider, string filePath) = getFileInfo(state, urlHelper);
            IFileInfo fileInfo = fileProvider.GetFileInfo(filePath);
            if (fileInfo.Exists)
            {
                cacheEntryOptions.AddExpirationToken(fileProvider.Watch(filePath));
                version = GetHashForFile(fileInfo);
            }
            else
                version = null;

            cacheEntryOptions.SetSize((version ?? string.Empty).Length * sizeof(char));
            return _cache.Set(url, version, cacheEntryOptions);
        }

        private string AddVersionCore(string url, string version)
        {
            if (version == null)
                return url;

            var index = url.IndexOf('?');
            StringSegment leftPart = index >= 0 ? new StringSegment(url, 0, index) : url;

            UrlUtils.DeconstructPath(new StringSegment(url, leftPart.Length, url.Length - leftPart.Length), out _, out QueryString query, out FragmentString fragment);

            QueryStringVersioningBundleUrlHelper.AddVersion(version, ref query);

            return leftPart.AsSpan().Concat(query.ToString().AsSpan(), fragment.ToString().AsSpan());
        }

        public string AddVersion(string url, IUrlHelper urlHelper, StaticFileUrlToFileMapper mapper)
        {
            var version = GetVersion(url, urlHelper, (url, mapper), (state, uh) =>
                state.mapper(state.url, uh, out IFileProvider fileProvider, out string filePath, out _) ?
                    (fileProvider, filePath) :
                    (AbstractionFile.NullFileProvider, null));

            return AddVersionCore(url, version);
        }

        public string AddVersion(string url, IUrlHelper urlHelper, IFileProvider fileProvider, string filePath)
        {
            var version = GetVersion(url, urlHelper, (fileProvider, filePath), (state, _) => state);

            return AddVersionCore(url, version);
        }
    }
}
