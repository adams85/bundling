using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.FileProviders;

namespace Karambolo.AspNetCore.Bundling.Internal.DesignTime
{
    internal class ConfigFileConfiguration : DesignTimeBundlingConfiguration
    {
        public string ConfigFilePath { get; set; }

        private IEnumerable<IBundlingModule> _modules;
        public override IEnumerable<IBundlingModule> Modules => _modules;

        public void SetModules(IEnumerable<IBundlingModule> modules)
        {
            _modules = modules;
        }

        public override void Configure(BundleCollectionConfigurer bundles)
        {
            var fileProvider = new PhysicalFileProvider(Path.GetDirectoryName(ConfigFilePath));
            bundles.LoadFromConfigFile(Path.GetFileName(ConfigFilePath), fileProvider);
        }
    }
}
