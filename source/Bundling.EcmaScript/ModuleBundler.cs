using System.Threading;
using System.Threading.Tasks;

namespace Karambolo.AspNetCore.Bundling.EcmaScript
{
    public interface IModuleBundler
    {
        Task<ModuleBundlingResult> BundleAsync(ModuleFile[] rootFiles, CancellationToken cancellationToken = default);
    }
}
