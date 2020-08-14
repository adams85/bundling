using System;

namespace Karambolo.AspNetCore.Bundling.Internal
{
    public class BundlingErrorException : Exception
    {
        public BundlingErrorException(string message) : base(message) { }

        public BundlingErrorException(string message, Exception innerException) : base(message, innerException) { }
    }
}
