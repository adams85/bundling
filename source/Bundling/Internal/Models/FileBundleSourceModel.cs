using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Karambolo.AspNetCore.Bundling.Internal.Helpers;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileSystemGlobbing;

namespace Karambolo.AspNetCore.Bundling.Internal.Models
{
    public class FileBundleSourceModel : IBundleSourceModel
    {
        protected class Include
        {
            public Include(FileBundleSourceItem item, string[] excludePatterns)
            {
                AutoDetectEncoding = item.InputEncoding == null;
                Encoding = item.InputEncoding ?? Encoding.UTF8;
                ItemTransforms = item.ItemTransforms;

                Matcher = new Matcher().AddInclude(item.Pattern);
                Array.ForEach(excludePatterns, p => Matcher.AddExclude(p));
            }

            public bool AutoDetectEncoding { get; }
            public Encoding Encoding { get; }
            public IReadOnlyList<IBundleItemTransform> ItemTransforms { get; }

            public Matcher Matcher { get; }
        }

        protected class BuildItem : BundleTransformContext, IBundleSourceBuildItem, IFileBundleItemTransformContext, IFileBundleSourceFilterItem
        {
            public BuildItem(IBundleBuildContext buildContext) : base(buildContext) { }

            public Include Include { get; set; }

            public IFileProvider FileProvider { get; set; }
            public bool CaseSensitiveFilePaths { get; set; }
            public string FilePath { get; set; }
            public IFileInfo FileInfo { get; set; }

            public IBundleItemTransformContext ItemTransformContext => this;
            public IReadOnlyList<IBundleItemTransform> ItemTransforms => Include.ItemTransforms;
        }

        private readonly IFileProvider _fileProvider;
        private readonly bool _caseSensitiveFilePaths;
        private readonly IReadOnlyList<IFileBundleSourceFilter> _fileFilters;
        private readonly Include[] _includes;

        public FileBundleSourceModel(FileBundleSource bundleSource)
        {
            _fileProvider =
                bundleSource.FileProvider ??
                throw ErrorHelper.PropertyNotSpecifed(nameof(FileBundleSource), nameof(FileBundleSource.FileProvider));
            _caseSensitiveFilePaths = bundleSource.CaseSensitiveFilePaths;

            _fileFilters = bundleSource.FileFilters;

            ILookup<bool, FileBundleSourceItem> lookup = bundleSource.Items.ToLookup(it => it.Exclude);

            var excludePatterns = lookup[true].Select(bsi => bsi.Pattern).ToArray();

            _includes = lookup[false].Select(bsi => CreateInclude(bsi, excludePatterns)).ToArray();
        }

        protected virtual Include CreateInclude(FileBundleSourceItem bundleSourceItem, string[] excludePatterns)
        {
            return new Include(bundleSourceItem, excludePatterns);
        }

        protected virtual BuildItem CreateBuildItem(Include include, string filePath, IBundleBuildContext context)
        {
            return new BuildItem(context)
            {
                Include = include,
                FileProvider = _fileProvider,
                CaseSensitiveFilePaths = _caseSensitiveFilePaths,
                FilePath = filePath,
            };
        }

        protected virtual Task CollectFilesAsync(List<IFileBundleSourceFilterItem> fileList, IBundleBuildContext context)
        {
            var directoryInfo = new GlobbingDirectoryInfo(_fileProvider, string.Empty);

            for (int i = 0, n = _includes.Length; i < n; i++)
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
            for (int i = 0, n = _fileFilters.Count; i < n; i++)
            {
                IFileBundleSourceFilter filter = _fileFilters[i];
                await filter.FilterAsync(fileList, context);
                filter.Filter(fileList, context);
            }
        }

        protected virtual async Task PostItemsAsync(List<IFileBundleSourceFilterItem> fileList, IBundleBuildContext context, Action<IBundleSourceBuildItem> processor)
        {
            var loadItemContent = (context as IBundleProvideBuildItemsContext)?.LoadItemContent ?? true;

            for (int i = 0, n = fileList.Count; i < n; i++)
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                var item = (BuildItem)fileList[i];

                item.FileInfo = item.FileProvider.GetFileInfo(item.FilePath);

                if (loadItemContent)
                {
                    using (Stream stream = item.FileInfo.CreateReadStream())
                    using (var reader = new StreamReader(stream, item.Include.Encoding, item.Include.AutoDetectEncoding))
                        item.Content = await reader.ReadToEndAsync();
                }

                processor(item);
            }
        }

        public async Task ProvideBuildItemsAsync(IBundleBuildContext context, Action<IBundleSourceBuildItem> processor)
        {
            var fileList = new List<IFileBundleSourceFilterItem>();

            await CollectFilesAsync(fileList, context);

            if (_fileFilters != null)
                await ExecuteFiltersAsync(fileList, context);

            context.ChangeSources?.UnionWith(fileList.Cast<BuildItem>().Select(bi => new AbstractionFile(bi.FileProvider, bi.FilePath, bi.CaseSensitiveFilePaths)));

            await PostItemsAsync(fileList, context, processor);
        }
    }
}
