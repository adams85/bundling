using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.FileProviders;

namespace Karambolo.AspNetCore.Bundling.Internal.DesignTime
{
    internal sealed class ConfigFileConfiguration : DesignTimeBundlingConfiguration
    {
        public ConfigFileConfiguration()
        {
            _modules = base.Modules;
        }

        public string ConfigFilePath { get; set; }

        private IEnumerable<IBundlingModule> _modules;
        public override IEnumerable<IBundlingModule> Modules => _modules;

        public void AddModules(IEnumerable<IBundlingModule> modules)
        {
            _modules = _modules.Concat(modules);
        }

        public override void Configure(BundleCollectionConfigurer bundles)
        {
            using (var fileProvider = new PhysicalFileProvider(Path.GetDirectoryName(ConfigFilePath)))
                bundles.LoadFromConfigFile(Path.GetFileName(ConfigFilePath), fileProvider);
        }
    }
}
