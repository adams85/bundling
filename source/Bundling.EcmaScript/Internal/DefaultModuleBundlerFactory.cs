using Microsoft.Extensions.Logging;

namespace Karambolo.AspNetCore.Bundling.EcmaScript.Internal
{
    public class DefaultModuleBundlerFactory : IModuleBundlerFactory
    {
        private readonly ILoggerFactory _loggerFactory;

        public DefaultModuleBundlerFactory(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

        public IModuleBundler Create(ModuleBundlerOptions options = null)
        {
            return new ModuleBundler(_loggerFactory, options);
        }
    }
}
