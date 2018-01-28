using System;
using System.Threading.Tasks;

namespace Karambolo.AspNetCore.Bundling.Internal
{
    public interface IBundleSourceModel
    {
        event EventHandler Changed;

        Task ProvideBuildItemsAsync(IBundleBuildContext context, Action<IBundleSourceBuildItem> processor);
    }
}
