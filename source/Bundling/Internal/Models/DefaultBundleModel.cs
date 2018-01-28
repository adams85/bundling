using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Karambolo.AspNetCore.Bundling.Internal.Helpers;
using Microsoft.AspNetCore.Http;

namespace Karambolo.AspNetCore.Bundling.Internal.Models
{
    public class DefaultBundleModel : IBundleModel
    {
        readonly IEnumerable<IBundleModelFactory> _modelFactories;

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
            var result = 
                _modelFactories.Select(f => f.CreateSource(bundleSource)).FirstOrDefault(m => m != null) ??
                throw ErrorHelper.ModelFactoryNotAvailable(bundleSource.GetType());

            result.Changed += SourceChanged;

            return result;
        }

        protected virtual void SourceChanged(object sender, EventArgs e)
        {
            OnChanged();
        }

        protected virtual void OnChanged()
        {
            Changed?.Invoke(this, EventArgs.Empty);
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
    }
}