using System;
using System.IO;
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
        Task<string> CompileAsync(string content, string virtualPathPrefix, string filePath, IFileProvider fileProvider, CancellationToken token);
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

        public Task<string> CompileAsync(string content, string virtualPathPrefix, string filePath, IFileProvider fileProvider, CancellationToken token)
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

            var result = engine.TransformToCss(content, fileName);

            if (!engine.LastTransformationSuccessful)
            {
                _logger.LogWarning($"Less compilation of '{{FILEPATH}}' failed:{Environment.NewLine}{{REASON}}",
                    (filePath ?? "(content)"),
                    (engine is LessEngine lessEngine ? lessEngine.LastTransformationError?.Message : null) ?? "Unknown reason.");

                result = string.Empty;
            }

            return Task.FromResult(result);
        }
    }
}
