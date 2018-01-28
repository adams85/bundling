using System;

namespace Karambolo.AspNetCore.Bundling
{
    public interface IBundleTransformContext
    {
        IBundleBuildContext BuildContext { get; }
        string Content { get; set; }
    }

    public class BundleTransformContext : IBundleTransformContext
    {
        public BundleTransformContext(IBundleBuildContext buildContext)
        {
            if (buildContext == null)
                throw new ArgumentNullException(nameof(buildContext));

            BuildContext = buildContext;
        }

        public IBundleBuildContext BuildContext { get; }
        public string Content { get; set; }
    }
}
