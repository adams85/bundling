using System;
using Esprima;
using Karambolo.AspNetCore.Bundling.Internal;
using Microsoft.Extensions.Logging;

namespace Karambolo.AspNetCore.Bundling.EcmaScript.Internal.Helpers
{
    internal static class EcmaScriptErrorHelper
    {
        internal const string CannotResolveNonRelativePathReason = $"Only relative import paths are supported by default. You may specify a custom import resolver via {nameof(ModuleBundlerOptions)}.{nameof(ModuleBundlerOptions.ImportResolver)}.";
        internal const string CannotResolveRelativePathWithoutFileProviderReason = "Relative import paths cannot be resolved without an associated file provider.";

        public static BundlingErrorException ResolvingImportSourceFailed(this ILogger logger, string moduleUrl, string sourceUrl, string reason)
        {
            const string messageFormat = "Failed to resolve import source '{1}' in the context of module '{0}'.";

            logger.LogError(string.Format(messageFormat, "{MODULEURL}", "{SOURCEURL}") + Environment.NewLine + "{REASON}", sourceUrl, moduleUrl, reason);

            return new BundlingErrorException(string.Format(messageFormat, moduleUrl, sourceUrl));
        }

        public static BundlingErrorException LoadingModuleFailed(this ILogger logger, string moduleUrl, Exception ex)
        {
            const string messageFormat = "Failed to load module '{0}'.";

            logger.LogError(string.Format(messageFormat, "{MODULEURL}") + Environment.NewLine + "{REASON}", moduleUrl, ex.Message);

            return new BundlingErrorException(string.Format(messageFormat, moduleUrl), ex);
        }

        public static BundlingErrorException ParsingModuleFailed(this ILogger logger, string moduleUrl, Exception ex)
        {
            if (ex is ParserException parserException && parserException.Error != null && parserException.Error.IsPositionDefined)
            {
                Position position = parserException.Error.Position;
                string reason = parserException.Error.Description;

                string messageFormat = "Failed to parse module '{0}'." + Environment.NewLine + "Error at {1}: {2}";

                logger.LogError(string.Format(messageFormat, "{MODULEURL}", "{POSITION}", "{REASON}"), moduleUrl, position, reason);

                return new BundlingErrorException(string.Format(messageFormat, moduleUrl, position, reason));
            }
            else
            {
                const string messageFormat = "Failed to parse module '{0}'.";

                logger.LogError(string.Format(messageFormat, "{MODULEURL}") + Environment.NewLine + "{REASON}", moduleUrl, ex.Message);

                return new BundlingErrorException(string.Format(messageFormat, moduleUrl), ex);
            }
        }

        public static BundlingErrorException RewritingModuleFailed(this ILogger logger, string moduleUrl, in Position position, string reason)
        {
            string messageFormat = "Failed to rewrite module '{0}'." + Environment.NewLine + "Error at {1}: {2}";

            logger.LogError(string.Format(messageFormat, "{MODULEURL}", "{POSITION}", "{REASON}"), moduleUrl, position, reason);

            return new BundlingErrorException(string.Format(messageFormat, moduleUrl, position, reason));
        }

        public static void NonRewritableDynamicImportWarning(this ILogger logger, string moduleUrl, in Position position)
        {
            logger.LogWarning("Non-rewritable dynamic import was found in module '{MODULEURL}' at {POSITION}.", moduleUrl, position);
        }

        public static void IgnoredImportAttributesWarning(this ILogger logger, string moduleUrl, in Position position)
        {
            logger.LogWarning("Since not supported currently, import attributes were ignored in module '{MODULEURL}' at {POSITION}.", moduleUrl, position);
        }
    }
}
