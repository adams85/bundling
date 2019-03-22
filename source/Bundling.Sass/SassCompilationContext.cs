using System;
using System.Threading;
using Microsoft.Extensions.FileProviders;

namespace Karambolo.AspNetCore.Bundling.Sass
{
    public sealed class SassCompilationContext : IDisposable
    {
        private static readonly NullFileProvider s_nullFileProvider = new NullFileProvider();

        internal SassCompilationContext(ISassCompiler compiler, string rootPath, IFileProvider fileProvider, CancellationToken cancellationToken)
        {
            Compiler = compiler;
            RootPath = rootPath;
            FileProvider = fileProvider ?? s_nullFileProvider;
            CancellationToken = cancellationToken;

            FileProviderFileManager.SetCompilationContext(this);
        }

        public void Dispose()
        {
            FileProviderFileManager.SetCompilationContext(null);
        }

        public ISassCompiler Compiler { get; }
        public string RootPath { get; }
        public IFileProvider FileProvider { get; }
        public CancellationToken CancellationToken { get; }
    }
}
