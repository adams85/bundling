using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Karambolo.AspNetCore.Bundling.Internal
{
    public class AbstractionFile : IChangeSource, IEquatable<AbstractionFile>
    {
        internal readonly struct FileProviderEqualityComparer : IEqualityComparer<IFileProvider>
        {
            private readonly StringComparer _pathComparer;

            public FileProviderEqualityComparer(StringComparer pathComparer)
            {
                _pathComparer = pathComparer;
            }

            public FileProviderEqualityComparer(bool caseSensitiveFilePaths): this(GetPathComparer(caseSensitiveFilePaths)) { }

            public bool Equals(IFileProvider fileProvider1, IFileProvider fileProvider2)
            {
                return 
                    fileProvider1 == fileProvider2 ||
                    fileProvider1 is PhysicalFileProvider physicalFileProvider1 && fileProvider2 is PhysicalFileProvider physicalFileProvider2 && 
                        _pathComparer.Equals(physicalFileProvider1.Root, physicalFileProvider2.Root);
            }

            public int GetHashCode(IFileProvider fileProvider)
            {
                return fileProvider is PhysicalFileProvider physicalFileProvider ? _pathComparer.GetHashCode(physicalFileProvider.Root) : fileProvider.GetHashCode();
            }
        }

        public static readonly NullFileProvider NullFileProvider = new NullFileProvider();

        internal static bool GetDefaultCaseSensitiveFilePaths(IFileProvider fileProvider)
        {
            return !(fileProvider is PhysicalFileProvider) || !RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        }

        internal static StringComparer GetPathComparer(bool caseSensitiveFilePaths)
        {
            return caseSensitiveFilePaths ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
        }

        public AbstractionFile(IFileProvider fileProvider, string filePath, bool caseSensitiveFilePaths = true)
        {
            FileProvider = fileProvider ?? NullFileProvider;
            FilePath = filePath;
            PathComparer = GetPathComparer(caseSensitiveFilePaths);
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
                new FileProviderEqualityComparer(PathComparer).Equals(FileProvider, other.FileProvider) &&
                PathComparer.Equals(FilePath, other.FilePath);
        }

        public bool Equals(IChangeSource other)
        {
            return Equals((object)other);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as AbstractionFile);
        }

        public override int GetHashCode()
        {
            int hashCode = -1540411083;
            hashCode = hashCode * -1521134295 + PathComparer.GetHashCode();
            hashCode = hashCode * -1521134295 + new FileProviderEqualityComparer(PathComparer).GetHashCode(FileProvider);
            hashCode = hashCode * -1521134295 + (FilePath != null ? PathComparer.GetHashCode(FilePath) : 0);
            return hashCode;
        }
    }
}
