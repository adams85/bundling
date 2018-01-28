using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace Karambolo.AspNetCore.Bundling
{
    public class BundleCollection : ICollection<Bundle>
    {
        readonly Dictionary<string, Bundle> _bundles;

        public BundleCollection() : this(PathString.Empty, null) { }

        public BundleCollection(PathString pathPrefix, IFileProvider sourceFileProvider)
        {
            PathPrefix = pathPrefix;
            SourceFileProvider = sourceFileProvider;

            _bundles = new Dictionary<string, Bundle>(StringComparer.OrdinalIgnoreCase);
        }

        public PathString PathPrefix { get; }
        public IFileProvider SourceFileProvider { get; }

        public int Count => _bundles.Count;

        public bool IsReadOnly => false;

        public void Clear()
        {
            _bundles.Clear();
        }

        public void Add(Bundle item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            _bundles[item.Path] = item;
        }

        public bool Remove(Bundle item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            return _bundles.Remove(item.Path);
        }

        bool ICollection<Bundle>.Contains(Bundle item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            return _bundles.ContainsValue(item);
        }

        void ICollection<Bundle>.CopyTo(Bundle[] array, int arrayIndex)
        {
            _bundles.Values.CopyTo(array, arrayIndex);
        }

        public IEnumerator<Bundle> GetEnumerator()
        {
            return _bundles.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class BundleCollectionConfigurer
    {
        readonly IOptionsMonitor<BundleDefaultsOptions> _defaultsOptions;

        public BundleCollectionConfigurer(BundleCollection bundles, IServiceProvider appServices)
        {
            if (bundles == null)
                throw new ArgumentNullException(nameof(bundles));

            if (appServices == null)
                throw new ArgumentNullException(nameof(appServices));

            _defaultsOptions = appServices.GetRequiredService<IOptionsMonitor<BundleDefaultsOptions>>();

            Bundles = bundles;
            AppServices = appServices;
        }

        public BundleCollection Bundles { get; }
        public IServiceProvider AppServices { get; }

        public BundleDefaultsOptions GetDefaults(string bundleType)
        {
            if (bundleType == null)
                throw new ArgumentNullException(nameof(bundleType));

            return _defaultsOptions.Get(bundleType);
        }
    }
}
