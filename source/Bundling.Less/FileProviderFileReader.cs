using System;
using System.IO;
using dotless.Core.Input;
using Microsoft.Extensions.FileProviders;

namespace Karambolo.AspNetCore.Bundling.Less
{
    public class FileProviderFileReader : IFileReader
    {
        public static readonly FileProviderFileReader Null = new FileProviderFileReader(new NullFileProvider());

        readonly IFileProvider _fileProvider;

        public FileProviderFileReader(IFileProvider fileProvider)
        {
            if (fileProvider == null)
                throw new ArgumentNullException(nameof(fileProvider));

            _fileProvider = fileProvider;
        }

        public bool UseCacheDependencies => false;

        public bool DoesFileExist(string fileName)
        {
            return _fileProvider.GetFileInfo(fileName).Exists;
        }

        public byte[] GetBinaryFileContents(string fileName)
        {
            using (var stream = _fileProvider.GetFileInfo(fileName).CreateReadStream())
            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                return ms.ToArray();
            }
        }

        public string GetFileContents(string fileName)
        {
            using (var stream = _fileProvider.GetFileInfo(fileName).CreateReadStream())
            using (var reader = new StreamReader(stream))
                return reader.ReadToEnd();
        }
    }
}
