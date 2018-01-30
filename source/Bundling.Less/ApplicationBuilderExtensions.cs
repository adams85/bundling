using System;
using System.Collections.Generic;
using System.IO;
using Karambolo.AspNetCore.Bundling;
using Karambolo.AspNetCore.Bundling.Css;
using Karambolo.AspNetCore.Bundling.Internal;
using Karambolo.AspNetCore.Bundling.Internal.Caching;
using Karambolo.AspNetCore.Bundling.Internal.Configuration;
using Karambolo.AspNetCore.Bundling.Internal.Helpers;
using Karambolo.AspNetCore.Bundling.Internal.Versioning;
using Karambolo.AspNetCore.Bundling.Js;
using Karambolo.AspNetCore.Bundling.Less;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder
{
    public static class ConfigurationExtensions
    {
        public static BundleConfigurer AddLess(this BundleCollectionConfigurer @this, PathString path)
        {
            var bundle = new Bundle(path, @this.GetDefaults(LessBundleConfiguration.BundleType));
            @this.Bundles.Add(bundle);
            return new BundleConfigurer(bundle, @this.Bundles.SourceFileProvider, @this.AppServices);
        }
    }
}
