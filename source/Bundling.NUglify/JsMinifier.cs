using System;
using Karambolo.AspNetCore.Bundling.Js;
using Microsoft.Extensions.Logging;
using NUglify;
using NUglify.JavaScript;

namespace Karambolo.AspNetCore.Bundling.NUglify
{
    public class JsMinifier : IJsMinifier
    {
        readonly CodeSettings _settings;
        readonly ILogger _logger;

        public JsMinifier(CodeSettings settings, ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null)
                throw new ArgumentNullException(nameof(loggerFactory));

            _settings = settings;
            _logger = loggerFactory.CreateLogger<JsMinifier>();
        }

        public string Process(string content, string filePath)
        {
            var result = Uglify.Js(content, _settings);

            if (result.Errors.Count > 0)
            {
                var message =
                    result.HasErrors ?
                    $"Js minification of '{(filePath ?? "n/a")}' failed:" :
                    $"Js minification of '{(filePath ?? "n/a")}' completed with warnings:";

                _logger.LogWarning(string.Concat(message , Environment.NewLine, "{REASON}"), string.Join(Environment.NewLine, result.Errors));

                if (result.HasErrors)
                    return content;
            }

            return result.Code;
        }
    }
}
