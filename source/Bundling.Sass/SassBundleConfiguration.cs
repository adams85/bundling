using System;
using System.Collections.Generic;
using System.Linq;
using Karambolo.AspNetCore.Bundling.Css;
using Karambolo.AspNetCore.Bundling.Internal.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Karambolo.AspNetCore.Bundling.Sass
{
    public static class SassBundleConfiguration
    {
        public const string BundleType = "sass";

        internal class Configurer : BundleDefaultsConfigurerBase<BundleDefaultsOptions>
        {
            public Configurer(Action<BundleDefaultsOptions, IServiceProvider> action, IServiceProvider serviceProvider)
                : base(action, serviceProvider) { }

            protected override string Name => BundleType;

            protected override void SetDefaults(BundleDefaultsOptions options)
            {
                IConfigurationHelper helper = ServiceProvider.GetRequiredService<IEnumerable<IConfigurationHelper>>().First(h => h.Type == BundleType);

                options.GlobalDefaults = ServiceProvider.GetRequiredService<IOptions<BundleGlobalOptions>>().Value;
                options.Type = BundleType;
                options.ConcatenationToken = "\n";

                options.ItemTransforms = helper.SetDefaultItemTransforms(options.GlobalDefaults.ItemTransforms);
                options.Transforms = helper.SetDefaultTransforms(options.GlobalDefaults.Transforms);

                options.ConfigurationHelper = helper;
            }
        }

        public class Helper : IConfigurationHelper
        {
            private readonly BundleGlobalOptions _globalOptions;
            private readonly SassCompileTransform _compileTransform;
            private readonly CssMinifyTransform _minifyTransform;

            public string Type => BundleType;

            public Helper(IOptions<BundleGlobalOptions> globalOptions, ISassCompiler compiler, ICssMinifier minifier)
            {
                _globalOptions = globalOptions.Value;
                _compileTransform = new SassCompileTransform(compiler);
                _minifyTransform = new CssMinifyTransform(minifier);
            }

            public virtual IReadOnlyList<IBundleItemTransform> SetDefaultItemTransforms(IReadOnlyList<IBundleItemTransform> itemTransforms)
            {
                return itemTransforms.ModifyIf(itemTransforms == null || !itemTransforms.Any(t => t is SassCompileTransform),
                    l =>
                    {
                        // url rewriting is done by the compiler
                        l.RemoveAll(t => t is CssRewriteUrlTransform);
                        l.Insert(0, _compileTransform);
                    });
            }

            public virtual IReadOnlyList<IBundleTransform> SetDefaultTransforms(IReadOnlyList<IBundleTransform> transforms)
            {
                return _globalOptions.EnableMinification ? EnableMinification(transforms) : transforms;
            }

            public virtual IReadOnlyList<IBundleTransform> EnableMinification(IReadOnlyList<IBundleTransform> transforms)
            {
                return transforms.ModifyIf(transforms == null || !transforms.Any(t => t is CssMinifyTransform),
                    l => l.Add(_minifyTransform));
            }
        }

        public class ExtensionMapper : IExtensionMapper
        {
            private readonly BundleDefaultsOptions _options;

            public ExtensionMapper(IOptionsMonitor<BundleDefaultsOptions> options)
            {
                _options = options.Get(BundleType);
            }

            public virtual IBundleConfiguration MapInput(string extension)
            {
                return 
                    ".sass".Equals(extension, StringComparison.OrdinalIgnoreCase) || ".scss".Equals(extension, StringComparison.OrdinalIgnoreCase) ? 
                    _options : 
                    null;
            }

            public virtual IBundleConfiguration MapOutput(string extension)
            {
                return null;
            }
        }
    }
}
