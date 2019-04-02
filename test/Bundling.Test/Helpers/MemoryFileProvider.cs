using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Karambolo.AspNetCore.Bundling.Internal.Helpers;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Karambolo.AspNetCore.Bundling.Test.Helpers
{
    internal class MemoryFileProvider : IFileProvider
    {
        private class File
        {
            public bool IsDirectory { get; set; }
            public StringBuilder Content { get; set; }
            public Encoding Encoding { get; set; }
            public CancellationTokenSource ChangeTokenSource { get; set; }
        }

        private static string NormalizePath(string path)
        {
            return UrlUtils.NormalizePath(path, PathNormalization.ExcludeSlash, canonicalize: true);
        }

        private readonly Dictionary<string, File> _catalog = new Dictionary<string, File>
        {
            { string.Empty, new File { IsDirectory = true } }
        };

        public bool Exists(string path)
        {
            path = NormalizePath(path);

            lock (_catalog)
                return _catalog.ContainsKey(path);
        }

        public bool IsDirectory(string path)
        {
            path = NormalizePath(path);

            lock (_catalog)
                return _catalog.TryGetValue(path, out File file) ? file.IsDirectory : false;
        }

        public Encoding GetEncoding(string path)
        {
            path = NormalizePath(path);

            lock (_catalog)
                return _catalog.TryGetValue(path, out File file) && !file.IsDirectory ? file.Encoding : null;
        }

        public int GetLength(string path)
        {
            path = NormalizePath(path);

            lock (_catalog)
                return ReadContent(path)?.Length ?? -1;
        }

        private void CheckPath(string path)
        {
            if (Exists(path))
                throw new InvalidOperationException($"A {(IsDirectory(path) ? "directory" : "name")} with the same name already exists.");

            var dir = Path.GetDirectoryName(path);
            if (!Exists(dir))
                throw new InvalidOperationException("Parent directory does not exist.");

            if (!IsDirectory(dir))
                throw new InvalidOperationException("Parent directory is a file.");
        }

        public void CreateDir(string path)
        {
            path = NormalizePath(path);

            lock (_catalog)
            {
                CheckPath(path);

                _catalog.Add(path, new File { IsDirectory = true });
            }
        }

        public void CreateFile(string path, string content = null, Encoding encoding = null)
        {
            path = NormalizePath(path);

            lock (_catalog)
            {
                CheckPath(path);

                _catalog.Add(path, new File { Content = new StringBuilder(content ?? string.Empty), Encoding = encoding });
            }
        }

        public string ReadContent(string path)
        {
            path = NormalizePath(path);

            lock (_catalog)
                return _catalog.TryGetValue(path, out File file) && !file.IsDirectory ? file.Content.ToString() : null;
        }

        public void WriteContent(string path, string content, bool append = false)
        {
            path = NormalizePath(path);

            CancellationTokenSource changeTokenSource = null;

            lock (_catalog)
            {
                if (!_catalog.TryGetValue(path, out File file))
                    throw new InvalidOperationException("File does not exist.");

                if (!append)
                    file.Content.Clear();

                file.Content.Append(content);

                if (file.ChangeTokenSource != null)
                {
                    changeTokenSource = file.ChangeTokenSource;
                    file.ChangeTokenSource = new CancellationTokenSource();
                }
            }

            changeTokenSource?.Cancel();
        }

        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            throw new NotImplementedException();
        }

        public IFileInfo GetFileInfo(string subpath)
        {
            subpath = NormalizePath(subpath);

            return new MemoryFileInfo(this, subpath);
        }

        public IChangeToken Watch(string filter)
        {
            if (filter.Contains("*"))
                throw new NotImplementedException();

            lock (_catalog)
            {
                if (!_catalog.TryGetValue(filter, out File file))
                    return new CancellationChangeToken(CancellationToken.None);

                if (file.ChangeTokenSource == null)
                    file.ChangeTokenSource = new CancellationTokenSource();

                return new CancellationChangeToken(file.ChangeTokenSource.Token);
            }
        }
    }
}
