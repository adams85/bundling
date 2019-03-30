using Microsoft.Extensions.DependencyInjection;

namespace Karambolo.AspNetCore.Bundling
{
    public sealed class WebMarkupMinBundlingModule : IBundlingModule
    {
        public BundlingConfigurer Configure(BundlingConfigurer configurer)
        {
            return configurer.UseWebMarkupMin();
        }
    }
}
