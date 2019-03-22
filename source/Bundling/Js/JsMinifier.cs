using System;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Karambolo.AspNetCore.Bundling.Js
{
    public interface IJsMinifier
    {
        string Process(string content, string filePath);
    }

    public class NullJsMinifier : IJsMinifier
    {
        private readonly ILogger _logger;
        private int _hasLoggedWarningFlag;

        public NullJsMinifier(ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null)
                throw new ArgumentNullException(nameof(loggerFactory));

            _logger = loggerFactory.CreateLogger<NullJsMinifier>();
        }

        public string Process(string content, string filePath)
        {
            if (Interlocked.CompareExchange(ref _hasLoggedWarningFlag, 1, 0) == 0)
                _logger.LogWarning($"Js minification is not performed because no actual implementation of the {nameof(IJsMinifier)} interface was registered. Install either the Karambolo.AspNetCore.Bundling.NUglify or the Karambolo.AspNetCore.Bundling.WebMarkupMin NuGet package and register it in your Startup.ConfigureServices method.");

            return content;
        }
    }
}
