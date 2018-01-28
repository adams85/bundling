using System;
using System.Collections.Generic;
using System.Text;
using Karambolo.AspNetCore.Bundling.Internal;
using Karambolo.AspNetCore.Bundling.Internal.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Karambolo.AspNetCore.Bundling
{
    public class Bundle : IBundleConfiguration
    {
        public Bundle(PathString path, IBundleConfiguration defaults)
        {
            if (!path.HasValue)
                throw ErrorHelper.ValueCannotBeEmpty(nameof(path));

            if (defaults == null)
                throw new ArgumentNullException(nameof(defaults));

            Path = path;
            Defaults = defaults;

            Sources = new List<BundleSource>();
        }

        public IBundleGlobalConfiguration GlobalDefaults => Defaults.GlobalDefaults;
        public IBundleConfiguration Defaults { get; }

        public string Type => Defaults.Type;
        public PathString Path { get; }

        public bool DependsOnParams { get; set; }

        public IList<BundleSource> Sources { get; }

        IBundleBuilder _builder;
        public IBundleBuilder Builder
        {
            get => _builder ?? Defaults.Builder ?? Defaults.GlobalDefaults.Builder;
            set => _builder = value;
        }

        IReadOnlyList<IFileBundleSourceFilter> _fileFilters;
        public IReadOnlyList<IFileBundleSourceFilter> FileFilters
        {
            get => _fileFilters ?? Defaults.FileFilters ?? Defaults.GlobalDefaults.FileFilters;
            set => _fileFilters = value;
        }

        IReadOnlyList<IBundleItemTransform> _itemTransforms;
        public IReadOnlyList<IBundleItemTransform> ItemTransforms
        {
            get => _itemTransforms ?? Defaults.ItemTransforms ?? Defaults.GlobalDefaults.ItemTransforms;
            set => _itemTransforms = value;
        }

        IReadOnlyList<IBundleTransform> _transforms;
        public IReadOnlyList<IBundleTransform> Transforms
        {
            get => _transforms ?? Defaults.Transforms ?? Defaults.GlobalDefaults.Transforms;
            set => _transforms = value;
        }

        string _concatenationToken;
        public string ConcatenationToken
        {
            get => _concatenationToken ?? Defaults.ConcatenationToken;
            set => _concatenationToken = value;
        }

        public Encoding OutputEncoding { get; set; }
        public IBundleCacheOptions CacheOptions { get; set; }

        public IConfigurationHelper ConfigurationHelper => Defaults.ConfigurationHelper;
    }

    public class BundleConfigurer
    {
        readonly Lazy<FileBundleSource> _bundleSource;

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

        public BundleConfigurer UseItemTransforms(Action<List<IBundleTransform>> modification)
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
            var helper =
                Bundle.ConfigurationHelper ??
                throw ErrorHelper.PropertyNotSpecifed(nameof(Bundling.Bundle), nameof(Bundling.Bundle.ConfigurationHelper));

            Bundle.Transforms = helper.EnableMinification(Bundle.Transforms);

            return this;
        }

        public BundleConfigurer DisableCaching()
        {
            var cacheOptions = new BundleCacheOptions(Bundle.CacheOptions);
            cacheOptions.NoCache = true;
            Bundle.CacheOptions = cacheOptions;

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
