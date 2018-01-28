using System;
using System.IO;
using Microsoft.Extensions.FileProviders;

namespace Karambolo.AspNetCore.Bundling.Internal.Caching
{
    public class MemoryFileInfo : IFileInfo
    {
        readonly byte[] _content;

        public MemoryFileInfo(string name, byte[] content, DateTimeOffset timestamp)
        {
            Name = name;
            _content = content;
            LastModified = timestamp;
        }

        public bool Exists => true;

        long IFileInfo.Length => _content.LongLength;

        public string PhysicalPath => null;

        public string Name { get; }

        public DateTimeOffset LastModified { get; }

        public bool IsDirectory => false;

        public Stream CreateReadStream()
        {
            return new MemoryStream(_content);
        }
    }
}
