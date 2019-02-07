using System;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Karambolo.AspNetCore.Bundling.Css
{
    public interface ICssMinifier
    {
        string Process(string content, string filePath);
    }

    public class NullCssMinifier : ICssMinifier
    {
        readonly ILogger _logger;
        int _hasLoggedWarningFlag;

        public NullCssMinifier(ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null)
                throw new ArgumentNullException(nameof(loggerFactory));

            _logger = loggerFactory.CreateLogger<NullCssMinifier>();
        }

        public string Process(string content, string filePath)
        {
            if (Interlocked.CompareExchange(ref _hasLoggedWarningFlag, 1, 0) == 0)
                _logger.LogWarning($"Css minification is not performed because no actual implementation of the {nameof(ICssMinifier)} interface was registered. Install either the Karambolo.AspNetCore.Bundling.NUglify or the Karambolo.AspNetCore.Bundling.WebMarkupMin NuGet package and register it in your Startup.ConfigureServices method.");

            return content;
        }
    }
}
