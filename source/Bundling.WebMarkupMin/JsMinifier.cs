using System;
using System.Linq;
using Karambolo.AspNetCore.Bundling.Js;
using Microsoft.Extensions.Logging;

using CrockfordJsMinifier = WebMarkupMin.Core.CrockfordJsMinifier;

namespace Karambolo.AspNetCore.Bundling.WebMarkupMin
{
    public class JsMinifier : IJsMinifier
    {
        private readonly ILogger _logger;
        private readonly CrockfordJsMinifier _minifier;

        public JsMinifier(ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null)
                throw new ArgumentNullException(nameof(loggerFactory));

            _logger = loggerFactory.CreateLogger<CssMinifier>();
            _minifier = new CrockfordJsMinifier();
        }

        public string Process(string content, string filePath)
        {
            global::WebMarkupMin.Core.CodeMinificationResult result = _minifier.Minify(content, isInlineCode: false);

            if (result.Errors.Count > 0)
            {
                _logger.LogWarning($"Js minification of '{{FILEPATH}}' failed:{Environment.NewLine}{{REASON}}",
                    filePath ?? "(content)",
                    result.Errors.Select(e => e.Message));

                return content;
            }
            else if (result.Warnings.Count > 0)
            {
                _logger.LogWarning($"Js minification of '{{FILEPATH}}' completed with warnings:{Environment.NewLine}{{REASON}}",
                    filePath ?? "(content)",
                    result.Warnings.Select(e => e.Message));
            }

            return result.MinifiedContent;
        }
    }
}
