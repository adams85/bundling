using System;
using System.Threading.Tasks;

namespace Karambolo.AspNetCore.Bundling.Internal
{
    public interface IBundleSourceModel
    {
        Task ProvideBuildItemsAsync(IBundleBuildContext context, Action<IBundleSourceBuildItem> processor);
    }
}
