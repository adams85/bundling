using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Karambolo.AspNetCore.Bundling.Internal.Helpers;
using Microsoft.Extensions.Primitives;

namespace Karambolo.AspNetCore.Bundling.Internal.Models
{
    public class DynamicBundleSourceModel : IBundleSourceModel
    {
        private readonly BuildItemsProvider _itemsProvider;
        private readonly IReadOnlyList<IBundleItemTransform> _itemTransforms;
        private readonly Func<IChangeToken> _changeTokenFactory;

        public DynamicBundleSourceModel(DynamicBundleSource bundleSource)
        {
            _itemsProvider =
                bundleSource.ItemsProvider ??
                throw ErrorHelper.PropertyNotSpecifed(nameof(DynamicBundleSource), nameof(DynamicBundleSource.ItemsProvider));

            _itemTransforms = bundleSource.ItemTransforms;

            _changeTokenFactory = bundleSource.ChangeTokenFactory;
        }

        public Task ProvideBuildItemsAsync(IBundleBuildContext context, Action<IBundleSourceBuildItem> processor)
        {
            context.ChangeSources?.Add(new FactoryChangeSource(_changeTokenFactory));

            return _itemsProvider(context, _itemTransforms, processor);
        }
    }
}
