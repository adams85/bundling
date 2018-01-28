using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Karambolo.AspNetCore.Bundling;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace DynamicBundle
{
    public class DynamicSource
    {
        readonly IFileProvider _sourceFileProvider;

        public DynamicSource(IFileProvider sourceFileProvider)
        {
            _sourceFileProvider = sourceFileProvider;

            // we are providing a change token so that the framework can detect changes to the imported less file
            ChangeTokenFactory = () => _sourceFileProvider.Watch("/less/*.less");
        }

        public Func<IChangeToken> ChangeTokenFactory { get; }

        public Task ProvideItems(IBundleBuildContext context, IReadOnlyList<IBundleItemTransform> itemTransforms, Action<IBundleSourceBuildItem> processor)
        {
            var color =
                context.Params != null && context.Params.TryGetValue("c", out StringValues values) && values.Count > 0 && !string.IsNullOrEmpty(values[0]) ?
                values[0] :
                "000";

            // we are providing an dynamic input item that depends on the value of the 'c' query string parameter
            var item = new BundleSourceBuildItem
            {
                ItemTransformContext = new FileBundleItemTransformContext(context)
                {
                    FileProvider = _sourceFileProvider,
                    FilePath = "/less/dummy.less",
                    Content =
$@"
@frame-color: #{color};
@import 'dynamic.less';
"
                },
                ItemTransforms = itemTransforms
            };

            processor(item);

            return Task.CompletedTask;
        }
    }
}
