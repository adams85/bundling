using System;
using System.Collections.Generic;
using System.Text;
using Karambolo.AspNetCore.Bundling.Internal.Helpers;
using Microsoft.Extensions.FileProviders;

namespace Karambolo.AspNetCore.Bundling
{
    public class FileBundleSource : BundleSource
    {
        public FileBundleSource(IFileProvider fileProvider, bool caseSensitiveFilePaths, Bundle bundle)
            : base(bundle)
        {
            if (fileProvider == null)
                throw new ArgumentNullException(nameof(fileProvider));

            FileProvider = fileProvider;
            CaseSensitiveFilePaths = caseSensitiveFilePaths;

            Items = new List<FileBundleSourceItem>();
        }

        public IFileProvider FileProvider { get; }
        public bool CaseSensitiveFilePaths { get; }

        public StringComparison PathComparisonType { get; set; }

        private IReadOnlyList<IFileBundleSourceFilter> _fileFilters;
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

        private IReadOnlyList<IBundleItemTransform> _itemTransforms;
        public IReadOnlyList<IBundleItemTransform> ItemTransforms
        {
            get => _itemTransforms ?? BundleSource.ItemTransforms;
            set => _itemTransforms = value;
        }
    }
}
