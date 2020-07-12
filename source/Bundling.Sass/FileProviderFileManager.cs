using System;
using System.IO;
using System.Threading;
using Karambolo.AspNetCore.Bundling.Sass.Internal.Helpers;
using LibSassHost;

namespace Karambolo.AspNetCore.Bundling.Sass
{
    public sealed class FileProviderFileManager : IFileManager
    {
        public static readonly FileProviderFileManager Instance = new FileProviderFileManager();

        private static readonly AsyncLocal<SassCompilationContext> s_compilationContext = new AsyncLocal<SassCompilationContext>();

        internal static void SetCompilationContext(SassCompilationContext context)
        {
            s_compilationContext.Value = context;
        }

        private FileProviderFileManager() { }

        private SassCompilationContext Context => s_compilationContext.Value ?? throw SassErrorHelper.CompilationContextNotAccessible();

        public bool SupportsConversionToAbsolutePath => false;

        public string GetCurrentDirectory()
        {
            return string.Empty;
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

            return path.StartsWith("/");
        }

        public string ToAbsolutePath(string path)
        {
            throw new NotSupportedException();
        }

        public string ReadFile(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            SassCompilationContext context = Context;

            context.CancellationToken.ThrowIfCancellationRequested();

            using (Stream stream = context.FileProvider.GetFileInfo(path).CreateReadStream())
            using (var reader = new StreamReader(stream))
                return reader.ReadToEnd();
        }
    }
}
