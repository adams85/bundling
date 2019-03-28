using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Karambolo.AspNetCore.Bundling.Internal.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Karambolo.AspNetCore.Bundling.Internal.Models
{
    public class DefaultBundleModel : ChangeObserver, IBundleModel
    {
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

            OutputEncoding = bundle.OutputEncoding ?? Encoding.UTF8;

            Builder =
                bundle.Builder ??
                throw ErrorHelper.PropertyNotSpecifed(nameof(Bundle), nameof(Bundle.Builder));

            Transforms = bundle.Transforms;

            CacheOptions = bundle.CacheOptions != null ? new BundleCacheOptions(bundle.CacheOptions) : BundleCacheOptions.Default;

            Sources = bundle.Sources.Select(CreateSourceModel).ToArray();
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
        public Encoding OutputEncoding { get; }
        public IBundleSourceModel[] Sources { get; }
        public IBundleBuilder Builder { get; }
        public IReadOnlyList<IBundleTransform> Transforms { get; }
        public IBundleCacheOptions CacheOptions { get; }

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

        public void OnBuilding(IBundleBuilderContext context)
        {
            if (context.ChangeSources != null)
                ResetChangeDetection(null);
        }

        public void OnBuilt(IBundleBuilderContext context)
        {
            if (context.ChangeSources != null)
                ResetChangeDetection(context.ChangeSources);
        }
    }
}
