using System;
using System.IO;
using System.Threading;
using dotless.Core.Input;
using Microsoft.Extensions.FileProviders;

namespace Karambolo.AspNetCore.Bundling.Less
{
    public class FileProviderFileReader : IFileReader
    {
        private readonly IFileProvider _fileProvider;
        private readonly CancellationToken _cancellationToken;

        public FileProviderFileReader(IFileProvider fileProvider, CancellationToken cancellationToken)
        {
            if (fileProvider == null)
                throw new ArgumentNullException(nameof(fileProvider));

            _fileProvider = fileProvider;
            _cancellationToken = cancellationToken;
        }

        public bool UseCacheDependencies => false;

        public bool DoesFileExist(string fileName)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            return _fileProvider.GetFileInfo(fileName).Exists;
        }

        public byte[] GetBinaryFileContents(string fileName)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            using (Stream stream = _fileProvider.GetFileInfo(fileName).CreateReadStream())
            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                return ms.ToArray();
            }
        }

        public string GetFileContents(string fileName)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            using (Stream stream = _fileProvider.GetFileInfo(fileName).CreateReadStream())
            using (var reader = new StreamReader(stream))
                return reader.ReadToEnd();
        }
    }
}
