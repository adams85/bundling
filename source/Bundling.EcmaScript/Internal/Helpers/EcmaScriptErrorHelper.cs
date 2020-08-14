using System;
using Esprima;
using Karambolo.AspNetCore.Bundling.Internal;
using Microsoft.Extensions.Logging;

namespace Karambolo.AspNetCore.Bundling.EcmaScript.Internal.Helpers
{
    internal static class EcmaScriptErrorHelper
    {
        public static BundlingErrorException ReadingModuleFileFailed(this ILogger logger, string moduleFilePath, string fileProviderHint, Exception ex)
        {
            const string messageFormat = "Failed to read file '{0}' via {1}.";

            logger.LogError(string.Format(messageFormat, "{FILEPATH}", "{FILEPROVIDER}") + Environment.NewLine + "{REASON}", moduleFilePath, fileProviderHint, ex.Message);

            return new BundlingErrorException(string.Format(messageFormat, moduleFilePath, fileProviderHint), ex);
        }

        public static BundlingErrorException ParsingModuleFileFailed(this ILogger logger, string moduleFilePath, string fileProviderHint, Exception ex)
        {
            const string messageFormat = "Failed to parse file '{0}' provided by {1}.";

            logger.LogError(string.Format(messageFormat, "{FILEPATH}", "{FILEPROVIDER}") + Environment.NewLine + "{REASON}", moduleFilePath, fileProviderHint, ex.Message);

            return new BundlingErrorException(string.Format(messageFormat, moduleFilePath, fileProviderHint), ex);
        }

        public static BundlingErrorException RewritingModuleFileFailed(this ILogger logger, string moduleFilePath, string fileProviderHint, in Position position, string reason)
        {
            string messageFormat = "Failed to rewrite file '{0}' provided by {1}." + Environment.NewLine + "Error at {2}: {3}";

            logger.LogError(string.Format(messageFormat, "{FILEPATH}", "{FILEPROVIDER}", "{POSITION}", "{REASON}"), moduleFilePath, fileProviderHint, position, reason);

            return new BundlingErrorException(string.Format(messageFormat, moduleFilePath, fileProviderHint, position, reason));
        }
    }
}
