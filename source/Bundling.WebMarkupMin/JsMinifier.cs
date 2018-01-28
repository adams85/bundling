using System;
using System.Linq;
using Karambolo.AspNetCore.Bundling.Js;
using Microsoft.Extensions.Logging;

using CrockfordJsMinifier = WebMarkupMin.Core.CrockfordJsMinifier;

namespace Karambolo.AspNetCore.Bundling.WebMarkupMin
{
    public class JsMinifier : IJsMinifier
    {
        readonly ILogger _logger;
        readonly CrockfordJsMinifier _minifier;

        public JsMinifier(ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null)
                throw new ArgumentNullException(nameof(loggerFactory));

            _logger = loggerFactory.CreateLogger<CssMinifier>();
            _minifier = new CrockfordJsMinifier();
        }

        public string Process(string content, string filePath)
        {
            var result = _minifier.Minify(content, isInlineCode: false);

            if (result.Errors.Count > 0)
            {
                var message = string.Concat($"Js minification of '{(filePath ?? "n/a")}' failed:", Environment.NewLine, "{REASON}");
                var reason = string.Join(Environment.NewLine, result.Errors.Select(e => e.Message));
                _logger.LogWarning(message, reason);

                return content;
            }
            else if (result.Warnings.Count > 0)
            {
                var message = string.Concat($"Js minification of '{(filePath ?? "n/a")}' completed with warnings:", Environment.NewLine, "{REASON}");
                var reason = string.Join(Environment.NewLine, result.Warnings.Select(e => e.Message));
                _logger.LogWarning(message, reason);
            }

            return result.MinifiedContent;
        }
    }
}
