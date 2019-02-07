using System;
using System.IO;
using System.Threading;
using LibSassHost;

namespace Karambolo.AspNetCore.Bundling.Sass
{
    public sealed class FileProviderFileManager : IFileManager
    {
        public static readonly FileProviderFileManager Instance = new FileProviderFileManager();

        static AsyncLocal<SassCompilationContext> compilationContext = new AsyncLocal<SassCompilationContext>();

        internal static void SetCompilationContext(SassCompilationContext context)
        {
            compilationContext.Value = context;
        }

        FileProviderFileManager() { }

        SassCompilationContext Context => compilationContext.Value ?? throw new InvalidOperationException("No ambient compilation context is accessible currently.");

        public bool SupportsConversionToAbsolutePath => false;

        public string GetCurrentDirectory()
        {
            return "/";
        }

        public bool FileExists(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            var context = Context;

            context.CancellationToken.ThrowIfCancellationRequested();

            var fileInfo = context.FileProvider.GetFileInfo(path);
            return fileInfo.Exists && !fileInfo.IsDirectory;
        }

        public bool IsAbsolutePath(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            return path.StartsWith('/');
        }

        public string ToAbsolutePath(string path)
        {
            throw new NotSupportedException();
        }

        string RewriteUrls(string content, string path)
        {
            var context = Context;

            var compiler = (SassCompiler)context.Compiler;
            path = compiler.GetBasePath(path, context.RootPath);

            return compiler.RewriteUrls(content, path, context);
        }

        public string ReadFile(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            var context = Context;

            context.CancellationToken.ThrowIfCancellationRequested();

            var fileInfo = context.FileProvider.GetFileInfo(path);
            using (var stream = fileInfo.CreateReadStream())
            using (var reader = new StreamReader(stream))
                return RewriteUrls(reader.ReadToEnd(), path);
        }
    }
}
