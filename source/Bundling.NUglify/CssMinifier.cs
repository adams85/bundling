using System;
using Karambolo.AspNetCore.Bundling.Css;
using Microsoft.Extensions.Logging;
using NUglify;
using NUglify.Css;

namespace Karambolo.AspNetCore.Bundling.NUglify
{
    public class CssMinifier : ICssMinifier
    {
        readonly CssSettings _settings;
        readonly ILogger _logger;

        public CssMinifier(CssSettings settings, ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null)
                throw new ArgumentNullException(nameof(loggerFactory));

            _settings = settings;
            _logger = loggerFactory.CreateLogger<CssMinifier>();
        }

        public string Process(string content, string filePath)
        {
            var result = Uglify.Css(content, _settings);

            if (result.Errors.Count > 0)
            {
                var message = 
                    result.HasErrors ?
                    $"Css minification of '{(filePath ?? "n/a")}' failed:" :
                    $"Css minification of '{(filePath ?? "n/a")}' completed with warnings:";

                _logger.LogWarning(string.Concat(message , Environment.NewLine, "{REASON}"), string.Join(Environment.NewLine, result.Errors));

                if (result.HasErrors)
                    return content;
            }

            return result.Code;
        }
    }
}
