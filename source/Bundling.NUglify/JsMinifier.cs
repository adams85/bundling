using System;
using Karambolo.AspNetCore.Bundling.Js;
using Microsoft.Extensions.Logging;
using NUglify;
using NUglify.JavaScript;

namespace Karambolo.AspNetCore.Bundling.NUglify
{
    public class JsMinifier : IJsMinifier
    {
        private readonly CodeSettings _settings;
        private readonly ILogger _logger;

        public JsMinifier(CodeSettings settings, ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null)
                throw new ArgumentNullException(nameof(loggerFactory));

            _settings = settings;
            _logger = loggerFactory.CreateLogger<JsMinifier>();
        }

        public string Process(string content, string filePath)
        {
            UglifyResult result = Uglify.Js(content, _settings);

            if (result.Errors.Count > 0)
            {
                var message =
                    result.HasErrors ?
                    $"Js minification of '{{FILEPATH}}' failed:{Environment.NewLine}{{REASON}}" :
                    $"Js minification of '{{FILEPATH}}' completed with warnings:{Environment.NewLine}{{REASON}}";

                _logger.LogWarning(message,
                    filePath ?? "(content)",
                    string.Join(Environment.NewLine, result.Errors));

                if (result.HasErrors)
                    return content;
            }

            return result.Code;
        }
    }
}
