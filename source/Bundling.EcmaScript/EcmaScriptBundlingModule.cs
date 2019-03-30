using Microsoft.Extensions.DependencyInjection;

namespace Karambolo.AspNetCore.Bundling
{
    public sealed class EcmaScriptBundlingModule : IBundlingModule
    {
        public BundlingConfigurer Configure(BundlingConfigurer configurer)
        {
            return configurer.AddEcmaScript();
        }
    }
}
