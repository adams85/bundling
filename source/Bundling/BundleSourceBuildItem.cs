using System.Collections.Generic;

namespace Karambolo.AspNetCore.Bundling
{
    public interface IBundleSourceBuildItem
    {
        IBundleItemTransformContext ItemTransformContext { get; }
        IReadOnlyList<IBundleItemTransform> ItemTransforms { get; }
    }

    public class BundleSourceBuildItem : IBundleSourceBuildItem
    {
        public IBundleItemTransformContext ItemTransformContext { get; set; }
        public IReadOnlyList<IBundleItemTransform> ItemTransforms { get; set; }
    }
}
