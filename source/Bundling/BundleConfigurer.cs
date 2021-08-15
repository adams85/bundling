using System;
using System.Collections.Generic;
using System.Text;
using Karambolo.AspNetCore.Bundling;
using Karambolo.AspNetCore.Bundling.Internal.Helpers;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Builder
{
    public class BundleConfigurer<TConfigurer>
        where TConfigurer : BundleConfigurer<TConfigurer>
    {
        private readonly Lazy<FileBundleSource> _bundleSource;

        public BundleConfigurer(Bundle bundle, IFileProvider sourceFileProvider, bool caseSensitiveSourceFilePaths, IServiceProvider appServices)
        {
            if (bundle == null)
                throw new ArgumentNullException(nameof(bundle));

            if (sourceFileProvider == null)
                throw new ArgumentNullException(nameof(sourceFileProvider));

            if (appServices == null)
                throw new ArgumentNullException(nameof(appServices));

            Bundle = bundle;
            AppServices = appServices;
            SourceFileProvider = sourceFileProvider;
            CaseSensitiveSourceFilePaths = caseSensitiveSourceFilePaths;

            _bundleSource = new Lazy<FileBundleSource>(() =>
            {
                var result = new FileBundleSource(SourceFileProvider, CaseSensitiveSourceFilePaths, bundle);
                AddSource(result);
                return result;
            }, isThreadSafe: false);
        }

        public Bundle Bundle { get; }
        public IServiceProvider AppServices { get; }
        public IFileProvider SourceFileProvider { get; }
        public bool CaseSensitiveSourceFilePaths { get; }

        public TConfigurer AddSource(BundleSource source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            Bundle.Sources.Add(source);
            return (TConfigurer)this;
        }

        public TConfigurer AddDynamicSource(BuildItemsProvider itemsProvider, Func<IChangeToken> changeTokenFactory = null,
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

            return (TConfigurer)this;
        }

        public TConfigurer DependsOnParams()
        {
            Bundle.DependsOnParams = true;
            return (TConfigurer)this;
        }

        public TConfigurer UseBuilder(IBundleBuilder builder)
        {
            Bundle.Builder = builder;
            return (TConfigurer)this;
        }

        public TConfigurer UseFileFilters(Action<List<IFileBundleSourceFilter>> modification)
        {
            _bundleSource.Value.FileFilters = _bundleSource.Value.FileFilters.Modify(modification);
            return (TConfigurer)this;
        }

        public TConfigurer UseItemTransforms(Action<List<IBundleItemTransform>> modification)
        {
            _bundleSource.Value.ItemTransforms = _bundleSource.Value.ItemTransforms.Modify(modification);
            return (TConfigurer)this;
        }

        public TConfigurer UseTransforms(Action<List<IBundleTransform>> modification)
        {
            Bundle.Transforms = Bundle.Transforms.Modify(modification);
            return (TConfigurer)this;
        }

        public TConfigurer UseConcatenationToken(string token)
        {
            Bundle.ConcatenationToken = token;
            return (TConfigurer)this;
        }

        public TConfigurer UseOutputEncoding(Encoding encoding)
        {
            Bundle.OutputEncoding = encoding;
            return (TConfigurer)this;
        }

        public TConfigurer UseCacheOptions(IBundleCacheOptions cacheOptions)
        {
            Bundle.CacheOptions = cacheOptions;
            return (TConfigurer)this;
        }

        public TConfigurer UseSourceItemUrlResolver(BundleSourceItemUrlResolver resolver)
        {
            Bundle.SourceItemUrlResolver = resolver;
            return (TConfigurer)this;
        }

        public TConfigurer EnableMinification()
        {
            IConfigurationHelper helper =
                Bundle.ConfigurationHelper ??
                throw ErrorHelper.PropertyNotSpecifed(nameof(Karambolo.AspNetCore.Bundling.Bundle), nameof(Karambolo.AspNetCore.Bundling.Bundle.ConfigurationHelper));

            Bundle.Transforms = helper.EnableMinification(Bundle.Transforms);

            return (TConfigurer)this;
        }

        public TConfigurer DisableCaching()
        {
            Bundle.CacheOptions = new BundleCacheOptions(Bundle.CacheOptions) { NoCache = true };

            return (TConfigurer)this;
        }

        public TConfigurer Include(string pattern, Action<List<IBundleItemTransform>> transformsModification = null, Encoding inputEncoding = null)
        {
            var item = new FileBundleSourceItem(pattern, _bundleSource.Value) { InputEncoding = inputEncoding };

            if (transformsModification != null)
                item.ItemTransforms = item.ItemTransforms.Modify(transformsModification);

            _bundleSource.Value.Items.Add(item);

            return (TConfigurer)this;
        }

        public TConfigurer Exclude(string pattern)
        {
            _bundleSource.Value.Items.Add(new FileBundleSourceItem(pattern, _bundleSource.Value) { Exclude = true });
            return (TConfigurer)this;
        }
    }
}
