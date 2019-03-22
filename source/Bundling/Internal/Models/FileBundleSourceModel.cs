using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Karambolo.AspNetCore.Bundling.Internal.Helpers;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Primitives;

namespace Karambolo.AspNetCore.Bundling.Internal.Models
{
    public class FileBundleSourceModel : IBundleSourceModel, IDisposable
    {
        protected class Include : ChangeTokenObserver
        {
            private readonly Action _changeCallback;

            public Include(FileBundleSourceItem item, string[] _excludePatterns, Func<IChangeToken> changeTokenFactory, Action changeCallback)
            {
                Pattern = item.Pattern;
                AutoDetectEncoding = item.InputEncoding == null;
                Encoding = item.InputEncoding ?? Encoding.UTF8;
                ItemTransforms = item.ItemTransforms;

                Matcher = new Matcher().AddInclude(item.Pattern);
                Array.ForEach(_excludePatterns, p => Matcher.AddExclude(p));

                _changeCallback = changeCallback;

                Initialize(changeTokenFactory);
            }

            public string Pattern { get; }
            public bool AutoDetectEncoding { get; }
            public Encoding Encoding { get; }
            public IReadOnlyList<IBundleItemTransform> ItemTransforms { get; }

            public Matcher Matcher { get; }

            protected override void OnChanged()
            {
                _changeCallback();
            }
        }

        protected class BuildItem : BundleTransformContext, IBundleSourceBuildItem, IFileBundleItemTransformContext, IFileBundleSourceFilterItem
        {
            public BuildItem(IBundleBuildContext buildContext) : base(buildContext) { }

            public Include Include { get; set; }

            public string FilePath { get; set; }
            public IFileProvider FileProvider { get; set; }
            public IFileInfo FileInfo { get; set; }

            public IBundleItemTransformContext ItemTransformContext => this;
            public IReadOnlyList<IBundleItemTransform> ItemTransforms => Include.ItemTransforms;
        }

        private readonly IFileProvider _fileProvider;
        private readonly StringComparison _pathComparisonType;
        private readonly IReadOnlyList<IFileBundleSourceFilter> _fileFilters;
        private readonly Include[] _includes;

        public FileBundleSourceModel(FileBundleSource bundleSource, bool enableChangeDetection)
        {
            _fileProvider =
                bundleSource.FileProvider ??
                throw ErrorHelper.PropertyNotSpecifed(nameof(FileBundleSource), nameof(FileBundleSource.FileProvider));

            _pathComparisonType = bundleSource.PathComparisonType;

            _fileFilters = bundleSource.FileFilters;

            ILookup<bool, FileBundleSourceItem> lookup = bundleSource.Items.ToLookup(it => it.Exclude);

            var excludePatterns = lookup[true].Select(bsi => bsi.Pattern).ToArray();

            // unfortunately file provider doesn't support exclude patterns and supports only a single include pattern
            // TODO: better solution?
            _includes = lookup[false].Select(bsi => CreateInclude(bsi, excludePatterns, enableChangeDetection)).ToArray();
        }

        protected virtual Include CreateInclude(FileBundleSourceItem bundleSourceItem, string[] excludePatterns, bool enableChangeDetection)
        {
            Func<IChangeToken> changeTokenFactory;
            if (enableChangeDetection)
                changeTokenFactory = () => _fileProvider.Watch(bundleSourceItem.Pattern);
            else
                changeTokenFactory = () => NullChangeToken.Singleton;

            return new Include(bundleSourceItem, excludePatterns, changeTokenFactory, OnChanged);
        }

        protected virtual BuildItem CreateBuildItem(Include include, string filePath, IBundleBuildContext context)
        {
            return new BuildItem(context)
            {
                Include = include,
                FilePath = filePath
            };
        }

        public void Dispose()
        {
            Array.ForEach(_includes, inc => inc.Dispose());
        }

        public event EventHandler Changed;

        protected virtual void OnChanged()
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }

        protected virtual Task CollectFilesAsync(List<IFileBundleSourceFilterItem> fileList, IBundleBuildContext context)
        {
            var directoryInfo = new GlobbingDirectoryInfo(_fileProvider, string.Empty);

            var n = _includes.Length;
            for (var i = 0; i < n; i++)
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                Include include = _includes[i];

                PatternMatchingResult matchingResult = include.Matcher.Execute(directoryInfo);
                if (matchingResult.HasMatches)
                    fileList.AddRange(matchingResult.Files.Select(m => CreateBuildItem(include, UrlUtils.NormalizePath(m.Path), context)));
            }

            return Task.CompletedTask;
        }

        protected virtual async Task ExecuteFiltersAsync(List<IFileBundleSourceFilterItem> fileList, IBundleBuildContext context)
        {
            var n = _fileFilters.Count;
            for (var i = 0; i < n; i++)
            {
                IFileBundleSourceFilter filter = _fileFilters[i];
                await filter.FilterAsync(fileList, context);
                filter.Filter(fileList, context);
            }
        }

        protected virtual async Task PostItemsAsync(List<IFileBundleSourceFilterItem> fileList, IBundleBuildContext context, Action<IBundleSourceBuildItem> processor)
        {
            var n = fileList.Count;
            for (var i = 0; i < n; i++)
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                var item = (BuildItem)fileList[i];
                item.FileProvider = _fileProvider;
                item.FileInfo = _fileProvider.GetFileInfo(item.FilePath);

                using (Stream stream = item.FileInfo.CreateReadStream())
                using (var reader = new StreamReader(stream, item.Include.Encoding, item.Include.AutoDetectEncoding))
                    item.Content = await reader.ReadToEndAsync();

                processor(item);
            }
        }

        public async Task ProvideBuildItemsAsync(IBundleBuildContext context, Action<IBundleSourceBuildItem> processor)
        {
            var fileList = new List<IFileBundleSourceFilterItem>();

            await CollectFilesAsync(fileList, context);

            if (_fileFilters != null)
                await ExecuteFiltersAsync(fileList, context);

            await PostItemsAsync(fileList, context, processor);
        }
    }
}
