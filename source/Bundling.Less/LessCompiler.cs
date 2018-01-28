using System;
using System.IO;
using System.Threading.Tasks;
using dotless.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

namespace Karambolo.AspNetCore.Bundling.Less
{
    public interface ILessCompiler
    {
        Task<string> CompileAsync(string content, string virtualPathPrefix, string filePath, IFileProvider fileProvider);
    }

    public class LessCompiler : ILessCompiler
    {
        readonly ILessEngineFactory _engineFactory;
        readonly ILogger _logger;

        public LessCompiler(ILessEngineFactory engineFactory, ILoggerFactory loggerFactory)
        {
            if (engineFactory == null)
                throw new ArgumentNullException(nameof(engineFactory));

            if (loggerFactory == null)
                throw new ArgumentNullException(nameof(loggerFactory));

            _engineFactory = engineFactory;
            _logger = loggerFactory.CreateLogger<LessCompiler>();
        }

        public Task<string> CompileAsync(string content, string virtualPathPrefix, string filePath, IFileProvider fileProvider)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));

            string fileBasePath, virtualBasePath, fileName;
            if (filePath != null)
            {
                fileBasePath = Path.GetDirectoryName(filePath).Replace('\\', '/');
                virtualBasePath = new PathString(virtualPathPrefix).Add(fileBasePath);
                fileName = Path.GetFileName(filePath);
            }
            else
                fileBasePath = virtualBasePath = fileName = null;

            var engine = _engineFactory.Create(fileBasePath ?? string.Empty, virtualBasePath ?? string.Empty, fileProvider);

            var result = engine.TransformToCss(content, fileName);

            if (!engine.LastTransformationSuccessful)
            {
                var message = string.Concat($"Less compilation of '{(filePath ?? "n/a")}' failed.");

                _logger.LogWarning(string.Concat(message, Environment.NewLine, "{REASON}"),
                    (engine is LessEngine lessEngine ? lessEngine.LastTransformationError?.Message : null) ?? "Unknown reason.");

                result = string.Empty;
            }

            return Task.FromResult(result);
        }
    }
}
