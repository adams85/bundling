using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Karambolo.AspNetCore.Bundling.EcmaScript.Internal.Helpers;
using Karambolo.AspNetCore.Bundling.Internal.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Karambolo.AspNetCore.Bundling.EcmaScript
{
    public interface IModuleResourceFactory
    {
        ModuleResource CreateFile(IFileProvider fileProvider, string filePath, bool caseSensitiveFilePaths, string content = null,
            QueryString query = default, FragmentString fragment = default);
        ModuleResource CreateTransient(string resourceId, string content, QueryString query = default, FragmentString fragment = default);
    }

    public abstract class ModuleResource : IEquatable<ModuleResource>
    {
        public abstract string Id { get; }

        private string _idEscaped;
        internal string IdEscaped => _idEscaped ??= HttpUtility.JavaScriptStringEncode(Id);

        public abstract Uri Url { get; }
        public virtual Uri DesensitizedUrl => Url;

        protected internal abstract Task<string> LoadContentAsync(CancellationToken token = default);

        public abstract bool TryResolveModule(string url, out string failureReason, out ModuleResource module);

        public abstract bool Equals(ModuleResource other);

        public sealed override bool Equals(object obj)
        {
            return Equals(obj as ModuleResource);
        }

        protected abstract int GetHashCodeImpl();

        public sealed override int GetHashCode()
        {
            return GetHashCodeImpl();
        }
    }

    internal sealed class TransientModuleResource : ModuleResource
    {
        private readonly string _resourceId;
        private readonly string _content;
        private readonly string _associatedFileProviderPrefix;
        private readonly IFileProvider _associatedFileProvider;
        private readonly bool _caseSensitiveFilePaths;
        private readonly string _normalizedParamPart;

        public TransientModuleResource(string resourceId, string content,
            string associatedFileProviderPrefix = null, IFileProvider associatedFileProvider = null, bool caseSensitiveFilePaths = true,
            QueryString query = default, FragmentString fragment = default)
        {
            _resourceId = resourceId;
            _content = content;
            _normalizedParamPart = UrlUtils.NormalizeQuery(query, out _).ToString() + fragment.ToString();

            if (associatedFileProviderPrefix != null && associatedFileProvider != null)
            {
                _associatedFileProviderPrefix = associatedFileProviderPrefix;
                _associatedFileProvider = associatedFileProvider;
                _caseSensitiveFilePaths = caseSensitiveFilePaths;
            }
        }

        public override string Id => _resourceId + _normalizedParamPart;

        public override Uri Url => new Uri("transient:" + Id);

        protected internal override Task<string> LoadContentAsync(CancellationToken token = default)
        {
            return Task.FromResult(_content);
        }

        public override bool TryResolveModule(string url, out string failureReason, out ModuleResource module)
        {
            UrlKind urlKind = UrlUtils.ClassifyUrl(url);
            if (urlKind != UrlKind.RelativeAndAbsolutePath && urlKind != UrlKind.RelativeAndRelativePath)
            {
                failureReason = EcmaScriptErrorHelper.CannotResolveNonRelativePathReason;
                module = default;
                return false;
            }

            if (_associatedFileProvider == null)
            {
                failureReason = EcmaScriptErrorHelper.CannotResolveRelativePathWithoutFileProviderReason;
                module = default;
                return false;
            }

            var filePath = FileModuleResource.ResolvePathToFilePath(url, urlKind == UrlKind.RelativeAndRelativePath, StringSegment.Empty, out QueryString query, out FragmentString fragment);

            failureReason = default;
            module = new FileModuleResource(_associatedFileProviderPrefix, _associatedFileProvider, filePath, _caseSensitiveFilePaths, content: null, query, fragment);
            return true;
        }

        public override bool Equals(ModuleResource other)
        {
            return other is TransientModuleResource otherResource &&
                _resourceId.Equals(otherResource._resourceId) &&
                _normalizedParamPart.Equals(otherResource._normalizedParamPart);
        }

        protected override int GetHashCodeImpl()
        {
            int hashCode = -1925768226;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(_resourceId);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(_normalizedParamPart);
            return hashCode;
        }
    }

    internal sealed class FileModuleResource : ModuleResource
    {
        public class AbstractionFileComparer : IEqualityComparer<FileModuleResource>
        {
            public bool Equals(FileModuleResource x, FileModuleResource y)
            {
                return x != null ? y != null && x._moduleFile.Equals(y._moduleFile) : y == null;
            }

            public int GetHashCode(FileModuleResource obj)
            {
                return obj?._moduleFile.GetHashCode() ?? 0;
            }
        }

        private static readonly char[] s_slashAndDot = new[] { '/', '.' };

        internal static StringSegment GetBasePath(string filePath)
        {
            UrlUtils.GetFileNameSegment(filePath, out StringSegment basePathSegment);
            return UrlUtils.NormalizePathSegment(basePathSegment, leadingNormalization: PathNormalization.None, trailingNormalization: PathNormalization.ExcludeSlash);
        }

        private readonly string _fileProviderPrefix;
        private readonly ModuleFile _moduleFile;
        private readonly string _normalizedParamPart;
        private readonly StringSegment _basePath;

        internal FileModuleResource(string fileProviderPrefix, IFileProvider fileProvider, string normalizedFilePath, bool caseSensitiveFilePaths,
            string content = null, QueryString query = default, FragmentString fragment = default)
        {
            _fileProviderPrefix = fileProviderPrefix;
            _moduleFile = new ModuleFile(fileProvider, normalizedFilePath, caseSensitiveFilePaths) { Content = content };
            _normalizedParamPart = UrlUtils.NormalizeQuery(query, out _).ToString() + fragment.ToString();
            _basePath = GetBasePath(normalizedFilePath);
        }

        private FileModuleResource(string fileProviderPrefix, FileModuleResource origin, string normalizedFilePath, QueryString query, FragmentString fragment)
            : this(fileProviderPrefix, origin.FileProvider, normalizedFilePath, origin.ModuleFile.CaseSensitiveFilePaths, content: null, query, fragment) { }

        public FileModuleResource(string fileProviderPrefix, ModuleFile moduleFile)
            : this(fileProviderPrefix, moduleFile.FileProvider, UrlUtils.NormalizePath(UrlUtils.NormalizeDirectorySeparators(moduleFile.FilePath)),
                  moduleFile.CaseSensitiveFilePaths, moduleFile.Content)
        { }

        public override string Id => _fileProviderPrefix + FilePath + _normalizedParamPart;

        public override Uri Url =>
            FileProvider is PhysicalFileProvider physicalFileProvider ?
            new Uri(Uri.UriSchemeFile + "://" + Path.Combine(physicalFileProvider.Root, FilePath.Substring(1)) + _normalizedParamPart) :
            DesensitizedUrl;

        public override Uri DesensitizedUrl => new Uri("provider-file:" + FileProvider.GetType().Name + _fileProviderPrefix + FilePath + _normalizedParamPart);

        public ModuleFile ModuleFile => _moduleFile;
        public IFileProvider FileProvider => _moduleFile.FileProvider;
        public string FilePath => _moduleFile.FilePath;

        protected internal override async Task<string> LoadContentAsync(CancellationToken token = default)
        {
            if (_moduleFile.Content != null)
                return _moduleFile.Content;

            using (Stream stream = _moduleFile.GetFileInfo().CreateReadStream())
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

        public override bool TryResolveModule(string url, out string failureReason, out ModuleResource module)
        {
            UrlKind urlKind = UrlUtils.ClassifyUrl(url);
            if (urlKind != UrlKind.RelativeAndAbsolutePath && urlKind != UrlKind.RelativeAndRelativePath)
            {
                failureReason = EcmaScriptErrorHelper.CannotResolveNonRelativePathReason;
                module = default;
                return false;
            }

            var filePath = ResolvePathToFilePath(url, urlKind == UrlKind.RelativeAndRelativePath, _basePath, out QueryString query, out FragmentString fragment);

            failureReason = default;
            module = new FileModuleResource(_fileProviderPrefix, this, filePath, query, fragment);
            return true;
        }

        public override bool Equals(ModuleResource other)
        {
            return other is FileModuleResource otherResource &&
                _moduleFile.Equals(otherResource._moduleFile) &&
                _normalizedParamPart.Equals(otherResource._normalizedParamPart);
        }

        protected override int GetHashCodeImpl()
        {
            int hashCode = -1046547636;
            hashCode = hashCode * -1521134295 + _moduleFile.GetHashCode();
            hashCode = hashCode * -1521134295 + _normalizedParamPart.GetHashCode();
            return hashCode;
        }
    }
}
