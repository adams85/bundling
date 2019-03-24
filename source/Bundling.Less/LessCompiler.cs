using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using dotless.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

namespace Karambolo.AspNetCore.Bundling.Less
{
    public interface ILessCompiler
    {
        Task<LessCompilationResult> CompileAsync(string content, string virtualPathPrefix, string filePath, IFileProvider fileProvider, CancellationToken token);
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

        public Task<LessCompilationResult> CompileAsync(string content, string virtualPathPrefix, string filePath, IFileProvider fileProvider, CancellationToken token)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));

            token.ThrowIfCancellationRequested();

            string fileBasePath, virtualBasePath, fileName;
            if (filePath != null)
            {
                fileBasePath = Path.GetDirectoryName(filePath).Replace('\\', '/');
                virtualBasePath = new PathString(virtualPathPrefix).Add(fileBasePath);
                fileName = Path.GetFileName(filePath);
            }
            else
                fileBasePath = virtualBasePath = fileName = null;

            ILessEngine engine = _engineFactory.Create(fileBasePath ?? string.Empty, virtualBasePath ?? string.Empty, fileProvider);

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
                new LessCompilationResult(content, engine.GetImports().ToArray()) :
                default);
        }
    }
}
