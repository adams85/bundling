using System;
using System.Threading;
using Karambolo.AspNetCore.Bundling.Internal;
using Microsoft.Extensions.FileProviders;

namespace Karambolo.AspNetCore.Bundling.Sass
{
    public sealed class SassCompilationContext : IDisposable
    {
        internal SassCompilationContext(ISassCompiler compiler, string rootPath, IFileProvider fileProvider, CancellationToken cancellationToken)
        {
            Compiler = compiler;
            RootPath = rootPath;
            FileProvider = fileProvider ?? AbstractionFile.NullFileProvider;
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
