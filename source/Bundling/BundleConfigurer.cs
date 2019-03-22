using System;
using System.Collections.Generic;
using System.Text;
using Karambolo.AspNetCore.Bundling;
using Karambolo.AspNetCore.Bundling.Internal.Helpers;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Builder
{
    public class BundleConfigurer
    {
        private readonly Lazy<FileBundleSource> _bundleSource;

        public BundleConfigurer(Bundle bundle, IFileProvider sourceFileProvider, IServiceProvider appServices)
        {
            if (bundle == null)
                throw new ArgumentNullException(nameof(bundle));

            if (sourceFileProvider == null)
                throw new ArgumentNullException(nameof(sourceFileProvider));

            if (appServices == null)
                throw new ArgumentNullException(nameof(appServices));

            Bundle = bundle;
            AppServices = appServices;

            _bundleSource = new Lazy<FileBundleSource>(() =>
            {
                var result = new FileBundleSource(sourceFileProvider, bundle);
                AddSource(result);
                return result;
            }, isThreadSafe: false);
        }

        public Bundle Bundle { get; }
        public IServiceProvider AppServices { get; }

        public BundleConfigurer AddSource(BundleSource source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            Bundle.Sources.Add(source);
            return this;
        }

        public BundleConfigurer AddDynamicSource(BuildItemsProvider itemsProvider, Func<IChangeToken> changeTokenFactory = null,
            Action<List<IBundleItemTransform>> itemTransformsModification = null)
        {
            if (itemsProvider == null)
                throw new ArgumentNullException(nameof(itemsProvider));

            var bundleSource = new DynamicBundleSource(Bundle)
            {
                ItemsProvider = itemsProvider,
                ChangeTokenFactory = changeTokenFactory
            };

            if (itemTransformsModification != null)
                bundleSource.ItemTransforms = bundleSource.ItemTransforms.Modify(itemTransformsModification);

            Bundle.Sources.Add(bundleSource);

            return this;
        }

        public BundleConfigurer DependsOnParams()
        {
            Bundle.DependsOnParams = true;
            return this;
        }

        public BundleConfigurer UseBuilder(IBundleBuilder builder)
        {
            Bundle.Builder = builder;
            return this;
        }

        public BundleConfigurer UseFileFilters(Action<List<IFileBundleSourceFilter>> modification)
        {
            _bundleSource.Value.FileFilters = _bundleSource.Value.FileFilters.Modify(modification);
            return this;
        }

        public BundleConfigurer UseItemTransforms(Action<List<IBundleItemTransform>> modification)
        {
            _bundleSource.Value.ItemTransforms = _bundleSource.Value.ItemTransforms.Modify(modification);
            return this;
        }

        public BundleConfigurer UseTransforms(Action<List<IBundleTransform>> modification)
        {
            Bundle.Transforms = Bundle.Transforms.Modify(modification);
            return this;
        }

        public BundleConfigurer UseConcatenationToken(string token)
        {
            Bundle.ConcatenationToken = token;
            return this;
        }

        public BundleConfigurer UseOutputEncoding(Encoding encoding)
        {
            Bundle.OutputEncoding = encoding;
            return this;
        }

        public BundleConfigurer UseCacheOptions(IBundleCacheOptions cacheOptions)
        {
            Bundle.CacheOptions = cacheOptions;
            return this;
        }

        public BundleConfigurer UsePathComparisonType(StringComparison comparisonType)
        {
            _bundleSource.Value.PathComparisonType = comparisonType;
            return this;
        }

        public BundleConfigurer EnableMinification()
        {
            IConfigurationHelper helper =
                Bundle.ConfigurationHelper ??
                throw ErrorHelper.PropertyNotSpecifed(nameof(Karambolo.AspNetCore.Bundling.Bundle), nameof(Karambolo.AspNetCore.Bundling.Bundle.ConfigurationHelper));

            Bundle.Transforms = helper.EnableMinification(Bundle.Transforms);

            return this;
        }

        public BundleConfigurer DisableCaching()
        {
            Bundle.CacheOptions = new BundleCacheOptions(Bundle.CacheOptions) { NoCache = true };

            return this;
        }

        public BundleConfigurer Include(string pattern, Action<List<IBundleItemTransform>> transformsModification = null, Encoding inputEncoding = null)
        {
            var item = new FileBundleSourceItem(pattern, _bundleSource.Value) { InputEncoding = inputEncoding };

            if (transformsModification != null)
                item.ItemTransforms = item.ItemTransforms.Modify(transformsModification);

            _bundleSource.Value.Items.Add(item);

            return this;
        }

        public BundleConfigurer Exclude(string pattern)
        {
            _bundleSource.Value.Items.Add(new FileBundleSourceItem(pattern, _bundleSource.Value) { Exclude = true });
            return this;
        }
    }
}
