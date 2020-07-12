using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Karambolo.AspNetCore.Bundling.Css;
using Karambolo.AspNetCore.Bundling.Internal.Helpers;
using Karambolo.AspNetCore.Bundling.Sass.Internal.Helpers;
using LibSassHost;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using SourcemapToolkit.SourcemapParser;

namespace Karambolo.AspNetCore.Bundling.Sass
{
    public interface ISassCompiler
    {
        Task<SassCompilationResult> CompileAsync(string content, PathString virtualPathPrefix, string filePath, IFileProvider fileProvider, PathString outputPath, CancellationToken token);
    }

    public class SassCompiler : ISassCompiler
    {
        private readonly struct SourcePositionComparer : IComparer<(SourcePosition, MappingEntry)>
        {
            public static readonly IComparer<(SourcePosition, MappingEntry)> BoxedInstance = new SourcePositionComparer();

            public int Compare((SourcePosition, MappingEntry) x, (SourcePosition, MappingEntry) y) => x.Item1.CompareTo(y.Item1);
        }

        private static readonly CompilationOptions s_compilationOptions = new CompilationOptions
        {
            SourceMap = true,
            OmitSourceMapUrl = true
        };

        private static MappingEntry FindSourceMapping(int captureIndex, List<(SourcePosition, MappingEntry)> sourceMappings, MultiLineTextHelper textHelper)
        {
            var capturePosition = new SourcePosition();
            (capturePosition.ZeroBasedLineNumber, capturePosition.ZeroBasedColumnNumber) = textHelper.MapToPosition(captureIndex);

            var index = sourceMappings.BinarySearch((capturePosition, null), SourcePositionComparer.BoxedInstance);

            if (index < 0)
            {
                index = ~index;
                if (index > 0)
                    index--;
            }

            return sourceMappings[index].Item2;
        }

        private readonly ILogger _logger;

        public SassCompiler(ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null)
                throw new ArgumentNullException(nameof(loggerFactory));

            _logger = loggerFactory.CreateLogger<SassCompiler>();
        }

        protected virtual string GetBasePath(string sourceFilePath, SassCompilationContext context)
        {
            StringSegment pathSegment =
                !sourceFilePath.StartsWith("/") ?
                UrlUtils.NormalizePathSegment(context.RootPath + (!context.RootPath.EndsWith("/") ? "/" : string.Empty) + sourceFilePath, canonicalize: true) :
                sourceFilePath;

            UrlUtils.GetFileNameSegment(pathSegment, out StringSegment basePathSegment);
            basePathSegment = UrlUtils.NormalizePathSegment(basePathSegment, trailingNormalization: PathNormalization.ExcludeSlash);
            return basePathSegment.Value;
        }

        protected virtual string RebaseUrl(string value, string basePath, SassCompilationContext context)
        {
            return CssRewriteUrlTransform.RebaseUrlCore(value, basePath, context.VirtualPathPrefix, context.OutputPath);
        }

        protected virtual string RewriteUrls(CompilationResult compilationResult, SassCompilationContext context)
        {
            SourceMap sourceMap = new SourceMapParser().ParseSourceMap(compilationResult.SourceMap);

            if (sourceMap.ParsedMappings.Count == 0)
                return compilationResult.CompiledContent;

            sourceMap.ParsedMappings.Sort((x, y) => x.GeneratedSourcePosition.CompareTo(y.GeneratedSourcePosition));

            var sourceMappings = sourceMap.ParsedMappings
                .Select(entry => (entry.GeneratedSourcePosition, entry))
                .ToList();

            var textHelper = new MultiLineTextHelper(compilationResult.CompiledContent);

            return CssRewriteUrlTransform.RewriteUrlsCore(compilationResult.CompiledContent, (url, capture) =>
            {
                MappingEntry sourceMapping = FindSourceMapping(capture.Index, sourceMappings, textHelper);
                var basePath = GetBasePath(sourceMapping.OriginalFileName, context);
                return RebaseUrl(url, basePath, context);
            });
        }

        public Task<SassCompilationResult> CompileAsync(string content, PathString virtualPathPrefix, string filePath, IFileProvider fileProvider, PathString outputPath, CancellationToken token)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));

            token.ThrowIfCancellationRequested();

            string fileBasePath;
            if (filePath != null)
            {
                filePath = UrlUtils.NormalizePath(filePath.Replace('\\', '/'));
                UrlUtils.GetFileNameSegment(filePath, out StringSegment basePathSegment);
                basePathSegment = UrlUtils.NormalizePathSegment(basePathSegment, trailingNormalization: PathNormalization.ExcludeSlash);
                fileBasePath = basePathSegment.Value;
            }
            else
                filePath = fileBasePath = "/";

            virtualPathPrefix = UrlUtils.NormalizePath(virtualPathPrefix, trailingNormalization: PathNormalization.ExcludeSlash);

            CompilationResult compilationResult;
            using (var context = new SassCompilationContext(this, fileBasePath, virtualPathPrefix, fileProvider, outputPath, token))
            {
                try
                {
                    compilationResult = LibSassHost.SassCompiler.Compile(content, filePath, options: s_compilationOptions);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Sass compilation of '{{FILEPATH}}' failed.{Environment.NewLine}{{REASON}}",
                        (filePath ?? "(content)"),
                        ex.Message);

                    compilationResult = null;
                }

                return Task.FromResult(
                    compilationResult != null ?
                    new SassCompilationResult(RewriteUrls(compilationResult, context), compilationResult.IncludedFilePaths) :
                    SassCompilationResult.Failure);
            }
        }
    }
}
