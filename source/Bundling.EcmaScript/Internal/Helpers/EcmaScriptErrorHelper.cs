using System;

namespace Karambolo.AspNetCore.Bundling.EcmaScript.Internal.Helpers
{
    internal static class EcmaScriptErrorHelper
    {
        public static ModuleBundlingErrorException ReadingModuleFileFailed(string moduleFilePath, string fileProviderHint, Exception ex)
        {
            return new ModuleBundlingErrorException($"Failed to read file {moduleFilePath} via {fileProviderHint}.", ex);
        }

        public static ModuleBundlingErrorException ParsingModuleFileFailed(string moduleFilePath, string fileProviderHint, Exception ex)
        {
            return new ModuleBundlingErrorException($"Failed to parse file {moduleFilePath} provided by {fileProviderHint}.", ex);
        }
    }
}
