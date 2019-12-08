using System;

namespace Karambolo.AspNetCore.Bundling.Tools
{
    [Flags]
    internal enum ConfigSources
    {
        None = 0,
        ConfigFile = 0x1,
        AppAssembly = 0x2,
        OutputAssemblies = 0x4,
        Default = ConfigFile | AppAssembly
    }
}
