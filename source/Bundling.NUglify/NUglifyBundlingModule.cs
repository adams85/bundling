using Microsoft.Extensions.DependencyInjection;
using NUglify.Css;
using NUglify.JavaScript;

namespace Karambolo.AspNetCore.Bundling
{
    public sealed class NUglifyBundlingModule : IBundlingModule
    {
        private readonly CssSettings _cssSettings;
        private readonly CodeSettings _jsSettings;

        public NUglifyBundlingModule() : this(null, null) { }

        public NUglifyBundlingModule(CssSettings cssSettings = null, CodeSettings jsSettings = null)
        {
            _cssSettings = cssSettings;
            _jsSettings = jsSettings;
        }

        public BundlingConfigurer Configure(BundlingConfigurer configurer)
        {
            return configurer.UseNUglify(_cssSettings, _jsSettings);
        }
    }
}
