using System;
using System.Collections.Generic;
using System.Text;
using Karambolo.AspNetCore.Bundling.Internal.Helpers;
using Microsoft.AspNetCore.Http;

namespace Karambolo.AspNetCore.Bundling
{
    public class Bundle : IBundleConfiguration, IRunTimeGlobalBundleConfiguration
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

        private IBundleBuilder _builder;
        public IBundleBuilder Builder
        {
            get => _builder ?? Defaults.Builder ?? GlobalDefaults.Builder;
            set => _builder = value;
        }

        private IReadOnlyList<IFileBundleSourceFilter> _fileFilters;
        public IReadOnlyList<IFileBundleSourceFilter> FileFilters
        {
            get => _fileFilters ?? Defaults.FileFilters ?? GlobalDefaults.FileFilters;
            set => _fileFilters = value;
        }

        private IReadOnlyList<IBundleItemTransform> _itemTransforms;
        public IReadOnlyList<IBundleItemTransform> ItemTransforms
        {
            get => _itemTransforms ?? Defaults.ItemTransforms ?? GlobalDefaults.ItemTransforms;
            set => _itemTransforms = value;
        }

        private IReadOnlyList<IBundleTransform> _transforms;
        public IReadOnlyList<IBundleTransform> Transforms
        {
            get => _transforms ?? Defaults.Transforms ?? GlobalDefaults.Transforms;
            set => _transforms = value;
        }

        private string _concatenationToken;
        public string ConcatenationToken
        {
            get => _concatenationToken ?? Defaults.ConcatenationToken;
            set => _concatenationToken = value;
        }

        public Encoding OutputEncoding { get; set; }
        public IBundleCacheOptions CacheOptions { get; set; }

        private bool? _renderSourceIncludes;
        public bool? RenderSourceIncludes
        {
            get =>
                _renderSourceIncludes ??
                (Defaults as IRunTimeGlobalBundleConfiguration)?.RenderSourceIncludes ??
                (GlobalDefaults as IRunTimeGlobalBundleConfiguration)?.RenderSourceIncludes;
            set => _renderSourceIncludes = value;
        }

        private BundleSourceItemToUrlMapper _sourceItemToUrlMapper;
        public BundleSourceItemToUrlMapper SourceItemToUrlMapper
        {
            get =>
                _sourceItemToUrlMapper ??
                (Defaults as IRunTimeGlobalBundleConfiguration)?.SourceItemToUrlMapper ??
                (GlobalDefaults as IRunTimeGlobalBundleConfiguration)?.SourceItemToUrlMapper;
            set => _sourceItemToUrlMapper = value;
        }

        public IConfigurationHelper ConfigurationHelper => Defaults.ConfigurationHelper;
    }
}
