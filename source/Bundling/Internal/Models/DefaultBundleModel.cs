using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Karambolo.AspNetCore.Bundling.Internal.Helpers;
using Karambolo.AspNetCore.Bundling.Internal.Rendering;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Karambolo.AspNetCore.Bundling.Internal.Models
{
#if NETSTANDARD2_0
    using IWebHostEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;
#else
    using Microsoft.AspNetCore.Hosting;
#endif

    public class DefaultBundleModel : ChangeObserver, IBundleModel
    {
        private static readonly BundleSourceItemUrlResolver s_defaultSourceItemUrlResolver =
            (IBundleSourceBuildItem item, IBundlingContext bundlingContext, IUrlHelper urlHelper, IWebHostEnvironment environment) =>
            item.ItemTransformContext is IFileBundleItemTransformContext fileItemContext &&
                new AbstractionFile(fileItemContext.FileProvider, fileItemContext.FilePath, fileItemContext.CaseSensitiveFilePaths).Equals(
                    new AbstractionFile(environment.WebRootFileProvider, fileItemContext.FilePath, fileItemContext.CaseSensitiveFilePaths)) ?
            urlHelper.Content("~" + bundlingContext.StaticFilesPathPrefix.Add(UrlUtils.NormalizePath(fileItemContext.FilePath)).ToString()) :
            null;

        private readonly IEnumerable<IBundleModelFactory> _modelFactories;

        public DefaultBundleModel(Bundle bundle, IEnumerable<IBundleModelFactory> modelFactories)
        {
            _modelFactories = modelFactories;

            Type = bundle.Type;
            Path = bundle.Path;

            DependsOnParams = bundle.DependsOnParams;

            ConcatenationToken =
                bundle.ConcatenationToken ??
                throw ErrorHelper.PropertyNotSpecifed(nameof(Bundle), nameof(Bundle.ConcatenationToken));

            OutputMediaType = bundle.ConfigurationHelper.OutputMediaType;

            OutputEncoding = bundle.OutputEncoding ?? Encoding.UTF8;

            Builder =
                bundle.Builder ??
                throw ErrorHelper.PropertyNotSpecifed(nameof(Bundle), nameof(Bundle.Builder));

            Transforms = bundle.Transforms;

            CacheOptions = bundle.CacheOptions != null ? new BundleCacheOptions(bundle.CacheOptions) : BundleCacheOptions.Default;

            var renderSourceIncludes = bundle.ConfigurationHelper.CanRenderSourceIncludes && (bundle.RenderSourceIncludes ?? false);
            HtmlRenderer = CreateHtmlRenderer(renderSourceIncludes);

            SourceItemUrlResolver = bundle.SourceItemUrlResolver ?? s_defaultSourceItemUrlResolver;

            Sources = bundle.Sources.Select(CreateSourceModel).ToArray();
        }

        protected virtual IBundleHtmlRenderer CreateHtmlRenderer(bool renderSourceIncludes)
        {
            if (renderSourceIncludes)
                return SourceIncludesBundleHtmlRenderer.Instance;

            return DefaultBundleHtmlRenderer.Instance;
        }

        protected virtual IBundleSourceModel CreateSourceModel(BundleSource bundleSource)
        {
            return
                _modelFactories.Select(f => f.CreateSource(bundleSource)).FirstOrDefault(m => m != null) ??
                throw ErrorHelper.ModelFactoryNotAvailable(bundleSource.GetType());
        }

        public string Type { get; }
        public PathString Path { get; }
        public bool DependsOnParams { get; }
        public string ConcatenationToken { get; }
        public string OutputMediaType { get; }
        public Encoding OutputEncoding { get; }
        public IBundleSourceModel[] Sources { get; }
        public IBundleBuilder Builder { get; }
        public IReadOnlyList<IBundleTransform> Transforms { get; }
        public IBundleCacheOptions CacheOptions { get; }
        public IBundleHtmlRenderer HtmlRenderer { get; }
        public BundleSourceItemUrlResolver SourceItemUrlResolver { get; }

        public event EventHandler Changed;

        protected override void OnChanged()
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }

        private void ResetChangeDetection(ISet<IChangeSource> changeSources)
        {
            if (changeSources != null)
                ResetChangeSource(() =>
                {
                    IChangeToken[] changeTokens = changeSources.Select(changeSource => changeSource.CreateChangeToken()).ToArray();
                    return
                        changeTokens.Length > 1 ? new CompositeChangeToken(changeTokens) :
                        changeTokens.Length == 1 ? changeTokens[0] :
                        NullChangeToken.Singleton;
                });
            else
                ResetChangeSource(() => NullChangeToken.Singleton);
        }

        public void OnBuilding(IBundleBuildContext context)
        {
            if (context.ChangeSources != null)
                ResetChangeDetection(null);
        }

        public void OnBuilt(IBundleBuildContext context)
        {
            if (context.ChangeSources != null)
                ResetChangeDetection(context.ChangeSources);
        }
    }
}
