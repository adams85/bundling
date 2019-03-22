using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Karambolo.AspNetCore.Bundling.Internal.Helpers;
using Microsoft.Extensions.FileProviders;

namespace Karambolo.AspNetCore.Bundling.Internal.Models
{
    public class DynamicBundleSourceModel : ChangeTokenObserver, IBundleSourceModel
    {
        private readonly BuildItemsProvider _itemsProvider;
        private readonly IReadOnlyList<IBundleItemTransform> _itemTransforms;

        public DynamicBundleSourceModel(DynamicBundleSource bundleSource, bool enableChangeDetection)
        {
            _itemTransforms = bundleSource.ItemTransforms;

            _itemsProvider =
                bundleSource.ItemsProvider ??
                throw ErrorHelper.PropertyNotSpecifed(nameof(DynamicBundleSource), nameof(DynamicBundleSource.ItemsProvider));

            Func<Microsoft.Extensions.Primitives.IChangeToken> changeTokenFactory =
                enableChangeDetection && bundleSource.ChangeTokenFactory != null ?
                bundleSource.ChangeTokenFactory :
                () => NullChangeToken.Singleton;

            Initialize(changeTokenFactory);
        }

        public event EventHandler Changed;

        protected override void OnChanged()
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }

        public Task ProvideBuildItemsAsync(IBundleBuildContext context, Action<IBundleSourceBuildItem> processor)
        {
            return _itemsProvider(context, _itemTransforms, processor);
        }
    }
}
