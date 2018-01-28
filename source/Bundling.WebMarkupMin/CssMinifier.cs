using System;
using System.Linq;
using Karambolo.AspNetCore.Bundling.Css;
using Microsoft.Extensions.Logging;

using KristensenCssMinifier = WebMarkupMin.Core.KristensenCssMinifier;

namespace Karambolo.AspNetCore.Bundling.WebMarkupMin
{
    public class CssMinifier : ICssMinifier
    {
        readonly ILogger _logger;
        readonly KristensenCssMinifier _minifier;

        public CssMinifier(ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null)
                throw new ArgumentNullException(nameof(loggerFactory));

            _logger = loggerFactory.CreateLogger<CssMinifier>();
            _minifier = new KristensenCssMinifier();
        }

        public string Process(string content, string filePath)
        {
            var result = _minifier.Minify(content, isInlineCode: false);

            if (result.Errors.Count > 0)
            {
                var message = string.Concat($"Css minification of '{(filePath ?? "n/a")}' failed:", Environment.NewLine, "{REASON}");
                var reason = string.Join(Environment.NewLine, result.Errors.Select(e => e.Message));
                _logger.LogWarning(message, reason);

                return content;
            }
            else if (result.Warnings.Count > 0)
            {
                var message = string.Concat($"Css minification of '{(filePath ?? "n/a")}' completed with warnings:", Environment.NewLine, "{REASON}");
                var reason = string.Join(Environment.NewLine, result.Warnings.Select(e => e.Message));
                _logger.LogWarning(message, reason);
            }

            return result.MinifiedContent;
        }
    }
}
