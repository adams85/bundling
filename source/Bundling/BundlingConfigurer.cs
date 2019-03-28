using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public class BundlingConfigurer
    {
        public BundlingConfigurer(IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            Services = services;
        }

        public IServiceCollection Services { get; }
    }
}
