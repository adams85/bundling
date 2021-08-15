namespace Karambolo.AspNetCore.Bundling.Internal
{
    public interface IBundleProvideBuildItemsContext 
    { 
        bool LoadItemContent { get; }
    }

    public class BundleProvideBuildItemsContext : BundleBuildContext, IBundleProvideBuildItemsContext
    {
        public bool LoadItemContent { get; set; }
    }
}
