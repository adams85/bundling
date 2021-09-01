using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Karambolo.AspNetCore.Bundling.EcmaScript.Internal.Helpers;
using Karambolo.AspNetCore.Bundling.Internal;
using Karambolo.AspNetCore.Bundling.Internal.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Karambolo.AspNetCore.Bundling.EcmaScript.Internal
{
    internal delegate IModuleResource ModuleResourceResolveErrorHandler<TState>(TState state, string url, string reason);

    internal interface IModuleResource : IEquatable<IModuleResource>
    {
        string Id { get; }
        Uri Url { get; }
        Uri SecureUrl { get; }

        Task<string> LoadContentAsync(CancellationToken token = default);
        IModuleResource Resolve<TState>(string url, TState state, ModuleResourceResolveErrorHandler<TState> errorHandler);
    }

    internal sealed class TransientModuleResource : IModuleResource
    {
        private readonly int _idNumber;
        private readonly string _content;
        private readonly string _associatedFileProviderPrefix;
        private readonly IFileProvider _associatedFileProvider;
        private readonly bool _caseSensitiveFilePaths;
        private readonly StringSegment _basePath;

        public TransientModuleResource(int id, string content, string associatedFileProviderPrefix, ModuleFile associatedModuleFile)
        {
            _idNumber = id;
            _content = content;

            if (associatedFileProviderPrefix != null && associatedModuleFile?.FileProvider != null)
            {
                _associatedFileProviderPrefix = associatedFileProviderPrefix;
                _associatedFileProvider = associatedModuleFile.FileProvider;
                _caseSensitiveFilePaths = associatedModuleFile.CaseSensitiveFilePaths;
                _basePath =
                    associatedModuleFile.FilePath != null ?
                    FileModuleResource.GetBasePath(UrlUtils.NormalizePath(UrlUtils.NormalizeDirectorySeparators(associatedModuleFile.FilePath))) :
                    StringSegment.Empty;
            }
        }

        private string _id;
        public string Id => _id ?? (_id = "<root" + _idNumber.ToString(CultureInfo.InvariantCulture) + ">");

        public Uri Url => new Uri("transient:" + Id);

        public Uri SecureUrl => Url;

        public Task<string> LoadContentAsync(CancellationToken token = default)
        {
            return Task.FromResult(_content);
        }

        public IModuleResource Resolve<TState>(string url, TState state, ModuleResourceResolveErrorHandler<TState> errorHandler)
        {
            UrlKind urlKind = UrlUtils.ClassifyUrl(url);
            if (urlKind != UrlKind.RelativeAndAbsolutePath && urlKind != UrlKind.RelativeAndRelativePath)
            {
                errorHandler(state, url, EcmaScriptErrorHelper.CannotResolveNonRelativePathReason);
                return null;
            }

            if (_associatedFileProvider == null)
            {
                errorHandler(state, url, EcmaScriptErrorHelper.CannotResolveRelativePathWithoutFileProviderReason);
                return null;
            }

            var filePath = FileModuleResource.ResolvePathToFilePath(url, urlKind == UrlKind.RelativeAndRelativePath, _basePath, out QueryString query, out FragmentString fragment);

            return new FileModuleResource(_associatedFileProviderPrefix, _associatedFileProvider, filePath, _caseSensitiveFilePaths, query, fragment);
        }

        public bool Equals(IModuleResource other)
        {
            return other is TransientModuleResource otherResource && _idNumber == otherResource._idNumber;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as IModuleResource);
        }

        public override int GetHashCode()
        {
            return _idNumber.GetHashCode();
        }
    }

    internal sealed class FileModuleResource : ModuleFile, IModuleResource
    {
        public readonly struct AbstractionFileComparer : IEqualityComparer<FileModuleResource>
        {
            public bool Equals(FileModuleResource x, FileModuleResource y)
            {
                return x != null ? y != null && x.Equals((AbstractionFile)y) : y == null;
            }

            public int GetHashCode(FileModuleResource obj)
            {
                return obj?.BaseGetHashCode() ?? 0;
            }
        }

        private static readonly char[] s_slashAndDot = new[] { '/', '.' };

        internal static StringSegment GetBasePath(string filePath)
        {
            UrlUtils.GetFileNameSegment(filePath, out StringSegment basePathSegment);
            return UrlUtils.NormalizePathSegment(basePathSegment, leadingNormalization: PathNormalization.None, trailingNormalization: PathNormalization.ExcludeSlash);
        }

        private readonly string _fileProviderPrefix;
        private readonly string _normalizedParamPart;
        private readonly StringSegment _basePath;

        internal FileModuleResource(string fileProviderPrefix, IFileProvider fileProvider, string normalizedFilePath, bool caseSensitiveFilePaths,
            QueryString query = default, FragmentString fragment = default)
            : base(fileProvider, normalizedFilePath, caseSensitiveFilePaths)
        {
            _fileProviderPrefix = fileProviderPrefix;
            _normalizedParamPart = UrlUtils.NormalizeQuery(query, out IDictionary<string, StringValues> _).ToString() + fragment.ToString();
            _basePath = GetBasePath(FilePath);
        }

        private FileModuleResource(string fileProviderPrefix, FileModuleResource origin, string normalizedFilePath, QueryString query, FragmentString fragment)
            : this(fileProviderPrefix, origin.FileProvider, normalizedFilePath, origin.CaseSensitiveFilePaths, query, fragment) { }

        public FileModuleResource(string fileProviderPrefix, ModuleFile moduleFile)
            : this(fileProviderPrefix, moduleFile.FileProvider, UrlUtils.NormalizePath(UrlUtils.NormalizeDirectorySeparators(moduleFile.FilePath)), moduleFile.CaseSensitiveFilePaths) { }

        private string _id;
        public string Id => _id ?? (_id = _fileProviderPrefix + FilePath + _normalizedParamPart);

        public Uri Url =>
            FileProvider is PhysicalFileProvider physicalFileProvider ?
            new Uri(Uri.UriSchemeFile + "://" + Path.Combine(physicalFileProvider.Root, FilePath.Substring(1)) + _normalizedParamPart) :
            SecureUrl;

        public Uri SecureUrl => new Uri("provider-file:" + FileProvider.GetType().Name + _fileProviderPrefix + FilePath + _normalizedParamPart);

        public async Task<string> LoadContentAsync(CancellationToken token = default)
        {
            if (Content != null)
                return Content;

            using (Stream stream = GetFileInfo().CreateReadStream())
            using (var reader = new StreamReader(stream))
                return await reader.ReadToEndAsync().ConfigureAwait(false);
        }

        internal static string ResolvePathToFilePath(string url, bool isRelativePath, StringSegment basePath, out QueryString query, out FragmentString fragment)
        {
            UrlUtils.DeconstructPath(url, out PathString pathString, out query, out fragment);
            var path = pathString.Value;

            var index = path.LastIndexOfAny(s_slashAndDot);
            if (index < 0 || path[index] != '.')
                path += ".js";

            return UrlUtils.NormalizePath(isRelativePath ? basePath.AsSpan().Concat(path.AsSpan()) : path, canonicalize: true);
        }

        public IModuleResource Resolve<TState>(string url, TState state, ModuleResourceResolveErrorHandler<TState> errorHandler)
        {
            UrlKind urlKind = UrlUtils.ClassifyUrl(url);
            if (urlKind != UrlKind.RelativeAndAbsolutePath && urlKind != UrlKind.RelativeAndRelativePath)
            {
                errorHandler(state, url, EcmaScriptErrorHelper.CannotResolveNonRelativePathReason);
                return null;
            }

            var filePath = ResolvePathToFilePath(url, urlKind == UrlKind.RelativeAndRelativePath, _basePath, out QueryString query, out FragmentString fragment);

            return new FileModuleResource(_fileProviderPrefix, this, filePath, query, fragment);
        }

        public bool Equals(IModuleResource other)
        {
            return other is FileModuleResource otherResource &&
                base.Equals(otherResource) &&
                _normalizedParamPart.Equals(otherResource._normalizedParamPart);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as IModuleResource);
        }

        public override int GetHashCode()
        {
            int hashCode = -1046547636;
            hashCode = hashCode * -1521134295 + base.GetHashCode();
            hashCode = hashCode * -1521134295 + _normalizedParamPart.GetHashCode();
            return hashCode;
        }

        private int BaseGetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
