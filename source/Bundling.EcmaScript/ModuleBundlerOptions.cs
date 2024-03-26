namespace Karambolo.AspNetCore.Bundling.EcmaScript
{
    public delegate ModuleResource ModuleImportResolver(string url, ModuleResource initiator, IModuleResourceFactory moduleResourceFactory);

    public class ModuleBundlerOptions
    {
        public string NewLine { get; set; }
        public bool DevelopmentMode { get; set; }
        public bool ExperimentalESFeatures { get; set; }
        public ModuleImportResolver ImportResolver { get; set; }
    }
}
