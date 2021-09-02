﻿using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Karambolo.AspNetCore.Bundling.Internal
{
    public interface IBundleModel
    {
        string Type { get; }
        PathString Path { get; }
        bool DependsOnParams { get; }
        string ConcatenationToken { get; }
        string OutputMediaType { get; }
        Encoding OutputEncoding { get; }
        IBundleSourceModel[] Sources { get; }
        IBundleBuilder Builder { get; }
        IReadOnlyList<IBundleTransform> Transforms { get; }
        IBundleCacheOptions CacheOptions { get; }
        IBundleHtmlRenderer HtmlRenderer { get; }
        BundleSourceItemToUrlMapper SourceItemToUrlMapper { get; }

        event EventHandler Changed;

        void OnBuilding(IBundleBuildContext context);
        void OnBuilt(IBundleBuildContext context);
    }
}
