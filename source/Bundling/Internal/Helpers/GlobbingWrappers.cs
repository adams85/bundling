using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace Karambolo.AspNetCore.Bundling.Internal.Helpers
{
    internal class GlobbingDirectoryInfo : DirectoryInfoBase
    {
        private readonly IFileProvider _fileProvider;
        private readonly string _path, _name;
        private readonly bool _isParentPath;

        public GlobbingDirectoryInfo(IFileProvider fileProvider, string fullPath)
            : this(fileProvider, !string.IsNullOrEmpty(fullPath) ? Path.GetDirectoryName(fullPath) : null, Path.GetFileName(fullPath)) { }

        public GlobbingDirectoryInfo(IFileProvider fileProvider, string path, string name)
        {
            _fileProvider = fileProvider;
            _path = path;
            _name = name;
        }

        private GlobbingDirectoryInfo(IFileProvider fileProvider, string fullPath, bool isParentPath)
            : this(fileProvider, fullPath)
        {
            _isParentPath = isParentPath;
        }

        public override IEnumerable<FileSystemInfoBase> EnumerateFileSystemInfos()
        {
            IDirectoryContents contents = _fileProvider.GetDirectoryContents(FullName);
            if (contents.Exists)
                foreach (IFileInfo item in contents)
                    if (item.IsDirectory)
                        yield return new GlobbingDirectoryInfo(_fileProvider, FullName, item.Name);
                    else
                        yield return new GlobbingFileInfo(_fileProvider, FullName, item.Name);
        }

        public override DirectoryInfoBase GetDirectory(string name)
        {
            var isParentPath = name == "..";
            if (!isParentPath)
            {
                IDirectoryContents contents = _fileProvider.GetDirectoryContents(FullName);
                IFileInfo item;
                return
                    // string comparison should depend on environment or some kind of setting,
                    // however it's irrelevant in this case as Matcher won't call this code
                    contents.Exists && (item = contents.FirstOrDefault(it => it.IsDirectory && it.Name.Equals(name, StringComparison.OrdinalIgnoreCase))) != null ?
                    new GlobbingDirectoryInfo(_fileProvider, FullName, item.Name) :
                    null;
            }
            else
                return new GlobbingDirectoryInfo(_fileProvider, _path, isParentPath);
        }

        public override FileInfoBase GetFile(string name)
        {
            return new GlobbingFileInfo(_fileProvider, FullName, name);
        }

        public override string Name => _isParentPath ? ".." : _name;

        public override string FullName => Path.Combine(_path ?? string.Empty, _name);

        public override DirectoryInfoBase ParentDirectory => new GlobbingDirectoryInfo(_fileProvider, _path);
    }

    internal class GlobbingFileInfo : FileInfoBase
    {
        private readonly IFileProvider _fileProvider;
        private readonly string _path, _name;

        public GlobbingFileInfo(IFileProvider fileProvider, string fullPath)
            : this(fileProvider, Path.GetDirectoryName(fullPath), Path.GetFileName(fullPath)) { }

        public GlobbingFileInfo(IFileProvider fileProvider, string path, string name)
        {
            _fileProvider = fileProvider;
            _path = path;
            _name = name;
        }

        public override string Name => _name;

        public override string FullName => Path.Combine(_path, _name);

        public override DirectoryInfoBase ParentDirectory => new GlobbingDirectoryInfo(_fileProvider, _path);
    }
}
