using System;
using Karambolo.AspNetCore.Bundling.Internal.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;

namespace Karambolo.AspNetCore.Bundling.EcmaScript.Internal
{
    internal sealed partial class ModuleBundler
    {
        private sealed class ModuleResourceFactory : IModuleResourceFactory
        {
            private readonly ModuleBundler _bundler;

            public ModuleResourceFactory(ModuleBundler bundler)
            {
                _bundler = bundler;
            }

            public ModuleResource CreateFile(IFileProvider fileProvider, string filePath, bool caseSensitiveFilePaths, string content, QueryString query, FragmentString fragment)
            {
                if (fileProvider == null)
                    throw new ArgumentNullException(nameof(fileProvider));

                if (filePath == null)
                    throw new ArgumentNullException(nameof(filePath));

                string fileProviderPrefix;
                lock (_bundler._fileProviderPrefixes)
                    fileProviderPrefix = _bundler.GetOrAddFileProviderPrefix(fileProvider);

                return new FileModuleResource(fileProviderPrefix, fileProvider, UrlUtils.NormalizePath(UrlUtils.NormalizeDirectorySeparators(filePath)),
                  caseSensitiveFilePaths, content, query, fragment);
            }

            public ModuleResource CreateTransient(string resourceId, string content, QueryString query, FragmentString fragment)
            {
                if (resourceId == null)
                    throw new ArgumentNullException(nameof(resourceId));

                if (resourceId.Length == 0)
                    throw ErrorHelper.ValueCannotBeEmpty(nameof(resourceId));

                return new TransientModuleResource(resourceId, content, query: query, fragment: fragment);
            }
        }
    }
}
