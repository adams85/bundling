using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Karambolo.AspNetCore.Bundling.Internal.Helpers;
using LibSassHost;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

namespace Karambolo.AspNetCore.Bundling.Sass
{
    public interface ISassCompiler
    {
        Task<string> CompileAsync(string content, string virtualPathPrefix, string filePath, IFileProvider fileProvider, CancellationToken token);
    }

    public class SassCompiler : ISassCompiler
    {
        static readonly Regex rewriteUrlsRegex = new Regex(
                @"(?<before>url\()(?<url>'\.{1,2}/[^']+'|""\.{1,2}/[^""]+""|\.{1,2}/[^)]+)(?<after>\))|" +
                @"(?<before>@import\s+)(?<url>'\.{1,2}/[^']+\.css'|""\.{1,2}/[^""]+\.css"")(?<after>\s*;)",
                RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        readonly ILogger _logger;

        public SassCompiler(ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null)
                throw new ArgumentNullException(nameof(loggerFactory));

            _logger = loggerFactory.CreateLogger<SassCompiler>();
        }

        protected internal virtual string GetBasePath(string filePath, string rootPath)
        {
            var endIndex = filePath.LastIndexOf('/');
            return filePath.Substring(rootPath.Length, endIndex - rootPath.Length + 1);
        }

        protected internal virtual string RebaseUrl(string value, string basePath, SassCompilationContext context)
        {
            return basePath + value;
        }

        protected internal virtual string RewriteUrls(string content, string basePath, SassCompilationContext context)
        {
            // 1) url values can usually be SASS expressions, there's no easy way to decide,
            // so only relative paths starting with "./" or "../" are rebased

            // 2) urls ending with '.css' must be rebased as they are not included
            // https://stackoverflow.com/questions/7111610/import-regular-css-file-in-scss-file

            return rewriteUrlsRegex.Replace(content,
                m =>
                {
                    var value = m.Groups["url"].Value;
                    var quote = StringUtils.RemoveQuotes(ref value);

                    return string.Concat(
                        m.Groups["before"].Value,
                        quote, RebaseUrl(value, basePath, context), quote,
                        m.Groups["after"].Value);
                });
        }

        public Task<string> CompileAsync(string content, string virtualPathPrefix, string filePath, IFileProvider fileProvider, CancellationToken token)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));

            token.ThrowIfCancellationRequested();

            string rootPath;
            if (filePath != null)
            {
                var index = filePath.LastIndexOf('/');
                rootPath = filePath.Substring(0, index + 1);
            }
            else
                filePath = rootPath = "/";

            CompilationResult compilationResult;
            using (var context = new SassCompilationContext(this, rootPath, fileProvider, token))
            {
                content = RewriteUrls(content, string.Empty, context);

                try
                {
                    compilationResult = LibSassHost.SassCompiler.Compile(content, filePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Sass compilation of '{{FILEPATH}}' failed.{Environment.NewLine}{{REASON}}",
                        (filePath ?? "(content)"),
                        ex.Message);

                    compilationResult = null;
                }
            }

            return Task.FromResult(compilationResult?.CompiledContent ?? string.Empty);
        }
    }
}
