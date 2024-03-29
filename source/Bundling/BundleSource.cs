﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Karambolo.AspNetCore.Bundling
{
    public abstract class BundleSource
    {
        protected BundleSource(Bundle bundle)
        {
            if (bundle == null)
                throw new ArgumentNullException(nameof(bundle));

            Bundle = bundle;
        }

        public Bundle Bundle { get; }

        private IReadOnlyList<IBundleItemTransform> _itemTransforms;
        public IReadOnlyList<IBundleItemTransform> ItemTransforms
        {
            get => _itemTransforms ?? Bundle.ItemTransforms;
            set => _itemTransforms = value;
        }

        public virtual bool AllowsSourceIncludes()
        {
            return ItemTransforms == null || ItemTransforms.All(t => t is IAllowsSourceIncludes);
        }
    }
}
