using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using dotless.Core;
using Karambolo.AspNetCore.Bundling.Internal.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Karambolo.AspNetCore.Bundling.Less
{
    public interface ILessCompiler
    {
        Task<LessCompilationResult> CompileAsync(string content, PathString virtualPathPrefix, string filePath, IFileProvider fileProvider, PathString targetPath, CancellationToken token);
    }

    public class LessCompiler : ILessCompiler
    {
        private readonly ILessEngineFactory _engineFactory;
        private readonly ILogger _logger;

        public LessCompiler(ILessEngineFactory engineFactory, ILoggerFactory loggerFactory)
        {
            if (engineFactory == null)
                throw new ArgumentNullException(nameof(engineFactory));

            if (loggerFactory == null)
                throw new ArgumentNullException(nameof(loggerFactory));

            _engineFactory = engineFactory;
            _logger = loggerFactory.CreateLogger<LessCompiler>();
        }

        public Task<LessCompilationResult> CompileAsync(string content, PathString virtualPathPrefix, string filePath, IFileProvider fileProvider, PathString outputPath, CancellationToken token)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));

            token.ThrowIfCancellationRequested();

            string fileBasePath, fileName;
            if (filePath != null)
            {
                filePath = UrlUtils.NormalizePath(filePath.Replace('\\', '/'));
                fileName = UrlUtils.GetFileNameSegment(filePath, out StringSegment basePathSegment).Value;
                basePathSegment = UrlUtils.NormalizePathSegment(basePathSegment, trailingNormalization: PathNormalization.ExcludeSlash);
                fileBasePath = basePathSegment.Value;
            }
            else
            {
                fileBasePath = "/";
                fileName = string.Empty;
            }

            ILessEngine engine = _engineFactory.Create(fileBasePath, virtualPathPrefix, fileProvider, outputPath, token);

            content = engine.TransformToCss(content, fileName);

            if (!engine.LastTransformationSuccessful)
            {
                _logger.LogWarning($"Less compilation of '{{FILEPATH}}' failed:{Environment.NewLine}{{REASON}}",
                    (filePath ?? "(content)"),
                    (engine is LessEngine lessEngine ? lessEngine.LastTransformationError?.Message : null) ?? "Unknown reason.");

                content = null;
            }

            return Task.FromResult(
                content != null ?
                new LessCompilationResult(content, engine.GetImports().Select(path => UrlUtils.NormalizePath(path, canonicalize: true)).ToArray()) :
                LessCompilationResult.Failure);
        }
    }
}
