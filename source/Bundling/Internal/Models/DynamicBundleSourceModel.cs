using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Karambolo.AspNetCore.Bundling.Internal.Helpers;
using Microsoft.Extensions.Primitives;

namespace Karambolo.AspNetCore.Bundling.Internal.Models
{
    public class DynamicBundleSourceModel : BundleSourceModelBase
    {
        private readonly BuildItemsProvider _itemsProvider;
        private readonly IReadOnlyList<IBundleItemTransform> _itemTransforms;
        private readonly Func<IChangeToken> _changeTokenFactory;

        public DynamicBundleSourceModel(DynamicBundleSource bundleSource, bool enableChangeDetection)
            : base(enableChangeDetection)
        {
            _itemsProvider =
                bundleSource.ItemsProvider ??
                throw ErrorHelper.PropertyNotSpecifed(nameof(DynamicBundleSource), nameof(DynamicBundleSource.ItemsProvider));

            _itemTransforms = bundleSource.ItemTransforms;

            if (enableChangeDetection)
                _changeTokenFactory = bundleSource.ChangeTokenFactory;
        }

        protected override IEnumerable<IChangeToken> GetChangeTokens(IBundleItemTransformContext context, List<string> filesToWatch)
        {
            IEnumerable<IChangeToken> changeTokens = base.GetChangeTokens(context, filesToWatch);

            if (_changeTokenFactory != null)
                changeTokens = changeTokens.Append(_changeTokenFactory());

            return changeTokens;
        }

        protected override Task ProvideBuildItemsCoreAsync(IBundleBuildContext context, Action<IBundleSourceBuildItem> processor, List<string> filesToWatch)
        {
            return _itemsProvider(context, _itemTransforms, processor);
        }
    }
}
