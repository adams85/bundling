using System;

namespace Karambolo.AspNetCore.Bundling
{
    public interface IBundleItemTransformContext
    {
        IBundleBuildContext BuildContext { get; }
        string Content { get; set; }
    }

    public class BundleItemTransformContext : IBundleItemTransformContext
    {
        public BundleItemTransformContext(IBundleBuildContext buildContext)
        {
            if (buildContext == null)
                throw new ArgumentNullException(nameof(buildContext));

            BuildContext = buildContext;
        }

        public IBundleBuildContext BuildContext { get; }
        public string Content { get; set; }
    }
}
