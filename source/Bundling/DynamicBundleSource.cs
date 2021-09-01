using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;

namespace Karambolo.AspNetCore.Bundling
{
    public delegate Task BuildItemsProvider(IBundleBuildContext context, IReadOnlyList<IBundleItemTransform> itemTransforms, Action<IBundleSourceBuildItem> processor);

    public class DynamicBundleSource : BundleSource
    {
        public DynamicBundleSource(Bundle bundle)
            : base(bundle) { }

        public BuildItemsProvider ItemsProvider { get; set; }
        public Func<IChangeToken> ChangeTokenFactory { get; set; }

        public override bool AllowsSourceIncludes()
        {
            return false;
        }
    }
}
