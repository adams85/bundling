using System;

namespace Karambolo.AspNetCore.Bundling.EcmaScript.Internal
{
    internal class ModuleBundlingErrorException : Exception
    {
        public ModuleBundlingErrorException(string message) : base(message) { }

        public ModuleBundlingErrorException(string message, Exception innerException) : base(message, innerException) { }
    }
}
