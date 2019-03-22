using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;

namespace Karambolo.AspNetCore.Bundling
{
    public class BundleCollection : ICollection<Bundle>
    {
        private readonly Dictionary<string, Bundle> _bundles;

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
}
