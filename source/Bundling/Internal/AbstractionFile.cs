using System;
using System.Runtime.InteropServices;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Karambolo.AspNetCore.Bundling.Internal
{
    public class AbstractionFile : IChangeSource, IEquatable<AbstractionFile>
    {
        public static readonly NullFileProvider NullFileProvider = new NullFileProvider();

        internal static bool GetDefaultCaseSensitiveFilePaths()
        {
            return !RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        }

        public AbstractionFile(IFileProvider fileProvider, string filePath, bool caseSensitiveFilePaths = true)
        {
            FileProvider = fileProvider ?? NullFileProvider;
            FilePath = filePath;
            PathComparer = caseSensitiveFilePaths ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
        }

        public IFileProvider FileProvider { get; }
        public string FilePath { get; }
        public bool CaseSensitiveFilePaths => PathComparer == StringComparer.Ordinal;
        public StringComparer PathComparer { get; }

        public IFileInfo GetFileInfo()
        {
            return FileProvider.GetFileInfo(FilePath);
        }

        public IChangeToken CreateChangeToken()
        {
            return FileProvider.Watch(FilePath);
        }

        public bool Equals(AbstractionFile other)
        {
            return
                other != null ?
                FileProvider == other.FileProvider && PathComparer == other.PathComparer && PathComparer.Equals(FilePath, other.FilePath) :
                false;
        }

        public bool Equals(IChangeSource other)
        {
            return Equals(other as AbstractionFile);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as AbstractionFile);
        }

        public override int GetHashCode()
        {
            return FileProvider.GetHashCode() ^ PathComparer.GetHashCode(FilePath) ^ PathComparer.GetHashCode();
        }
    }
}
