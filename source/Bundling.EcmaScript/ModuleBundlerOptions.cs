using System.Collections.Generic;

namespace Karambolo.AspNetCore.Bundling.EcmaScript
{
    public class ModuleBundlerOptions
    {
        public string NewLine { get; set; }
        public bool CaseSensitivePaths { get; set; } = true;
        public bool DevelopmentMode { get; set; }
    }
}
