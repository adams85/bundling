using System;
using System.IO;
using System.Threading;
using LibSassHost;

namespace Karambolo.AspNetCore.Bundling.Sass
{
    public sealed class FileProviderFileManager : IFileManager
    {
        public static readonly FileProviderFileManager Instance = new FileProviderFileManager();
        private static AsyncLocal<SassCompilationContext> s_compilationContext = new AsyncLocal<SassCompilationContext>();

        internal static void SetCompilationContext(SassCompilationContext context)
        {
            s_compilationContext.Value = context;
        }

        private FileProviderFileManager() { }

        private SassCompilationContext Context => s_compilationContext.Value ?? throw new InvalidOperationException("No ambient compilation context is accessible currently.");

        public bool SupportsConversionToAbsolutePath => false;

        public string GetCurrentDirectory()
        {
            return "/";
        }

        public bool FileExists(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            SassCompilationContext context = Context;

            context.CancellationToken.ThrowIfCancellationRequested();

            Microsoft.Extensions.FileProviders.IFileInfo fileInfo = context.FileProvider.GetFileInfo(path);
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

        private string RewriteUrls(string content, string path)
        {
            SassCompilationContext context = Context;

            var compiler = (SassCompiler)context.Compiler;
            path = compiler.GetBasePath(path, context.RootPath);

            return compiler.RewriteUrls(content, path, context);
        }

        public string ReadFile(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            SassCompilationContext context = Context;

            context.CancellationToken.ThrowIfCancellationRequested();

            Microsoft.Extensions.FileProviders.IFileInfo fileInfo = context.FileProvider.GetFileInfo(path);
            using (Stream stream = fileInfo.CreateReadStream())
            using (var reader = new StreamReader(stream))
                return RewriteUrls(reader.ReadToEnd(), path);
        }
    }
}
