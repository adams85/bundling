using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Karambolo.AspNetCore.Bundling.Internal.Helpers;
using Microsoft.Extensions.FileProviders;

namespace Karambolo.AspNetCore.Bundling
{
    public class FileBundleSource : BundleSource
    {
        static readonly StringComparison defaultPathComparisonType =
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
            StringComparison.OrdinalIgnoreCase :
            StringComparison.Ordinal;

        public FileBundleSource(IFileProvider fileProvider, Bundle bundle)
            : base(bundle)
        {
            if (fileProvider == null)
                throw new ArgumentNullException(nameof(fileProvider));

            FileProvider = fileProvider;
            PathComparisonType = defaultPathComparisonType;

            Items = new List<FileBundleSourceItem>();
        }

        public IFileProvider FileProvider { get; }

        public StringComparison PathComparisonType { get; set; }

        IReadOnlyList<IFileBundleSourceFilter> _fileFilters;
        public IReadOnlyList<IFileBundleSourceFilter> FileFilters
        {
            get => _fileFilters ?? Bundle.FileFilters;
            set => _fileFilters = value;
        }

        public IList<FileBundleSourceItem> Items { get; }
    }

    public class FileBundleSourceItem
    {
        public FileBundleSourceItem(string pattern, FileBundleSource bundleSource)
        {
            if (pattern == null)
                throw new ArgumentNullException(nameof(pattern));

            if (bundleSource == null)
                throw new ArgumentNullException(nameof(bundleSource));

            Pattern = UrlUtils.NormalizePath(pattern);

            BundleSource = bundleSource;
        }

        public FileBundleSource BundleSource { get; }

        public string Pattern { get; }
        public bool Exclude { get; set; }

        public Encoding InputEncoding { get; set; }

        IReadOnlyList<IBundleItemTransform> _itemTransforms;
        public IReadOnlyList<IBundleItemTransform> ItemTransforms
        {
            get => _itemTransforms ?? BundleSource.ItemTransforms;
            set => _itemTransforms = value;
        }
    }
}