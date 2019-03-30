using Microsoft.Extensions.DependencyInjection;

namespace Karambolo.AspNetCore.Bundling
{
    public interface IBundlingModule
    {
        BundlingConfigurer Configure(BundlingConfigurer configurer);
    }
}
