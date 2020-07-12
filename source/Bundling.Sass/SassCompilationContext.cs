using System;
using System.Threading;
using Karambolo.AspNetCore.Bundling.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;

namespace Karambolo.AspNetCore.Bundling.Sass
{
    public sealed class SassCompilationContext : IDisposable
    {
        internal SassCompilationContext(ISassCompiler compiler, string rootPath, PathString virtualPathPrefix, IFileProvider fileProvider, PathString outputPath, CancellationToken token)
        {
            Compiler = compiler;
            RootPath = rootPath;
            VirtualPathPrefix = virtualPathPrefix;
            FileProvider = fileProvider ?? AbstractionFile.NullFileProvider;
            OutputPath = outputPath;
            CancellationToken = token;

            FileProviderFileManager.SetCompilationContext(this);
        }

        public void Dispose()
        {
            FileProviderFileManager.SetCompilationContext(null);
        }

        public ISassCompiler Compiler { get; }
        public string RootPath { get; }
        public PathString VirtualPathPrefix { get; }
        public IFileProvider FileProvider { get; }
        public PathString OutputPath { get; }
        public CancellationToken CancellationToken { get; }
    }
}
