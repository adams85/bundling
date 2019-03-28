namespace Karambolo.AspNetCore.Bundling.EcmaScript
{
    public interface IModuleBundlerFactory
    {
        IModuleBundler Create(ModuleBundlerOptions options = null);
    }
}
