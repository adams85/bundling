using System;
using System.Runtime.InteropServices;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Karambolo.AspNetCore.Bundling.Internal
{
    public class AbstractionFile : IChangeSource, IEquatable<AbstractionFile>
    {
        public static readonly NullFileProvider NullFileProvider = new NullFileProvider();

        internal static bool GetDefaultCaseSensitiveFilePaths(IFileProvider fileProvider)
        {
            return !(fileProvider is PhysicalFileProvider) || !RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
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
            return other != null &&
                PathComparer == other.PathComparer &&
                (FileProvider == other.FileProvider ||
                 FileProvider is PhysicalFileProvider physicalFileProvider &&
                    other.FileProvider is PhysicalFileProvider otherPhysicalFileProvider &&
                    PathComparer.Equals(physicalFileProvider.Root, otherPhysicalFileProvider.Root)) &&
                PathComparer.Equals(FilePath, other.FilePath);
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
            int hashCode = -1540411083;
            hashCode = hashCode * -1521134295 + PathComparer.GetHashCode();
            hashCode = hashCode * -1521134295 + (FileProvider is PhysicalFileProvider physicalFileProvider ? PathComparer.GetHashCode(physicalFileProvider.Root) : FileProvider.GetHashCode());
            hashCode = hashCode * -1521134295 + (FilePath != null ? PathComparer.GetHashCode(FilePath) : 0);
            return hashCode;
        }
    }
}
