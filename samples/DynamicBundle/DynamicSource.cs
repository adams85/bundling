using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Karambolo.AspNetCore.Bundling;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace DynamicBundle
{
    public class DynamicSource
    {
        readonly IFileProvider _sourceFileProvider;
        readonly ILogger _logger;

        public DynamicSource(IFileProvider sourceFileProvider, ILoggerFactory loggerFactory)
        {
            _sourceFileProvider = sourceFileProvider;
            _logger = loggerFactory.CreateLogger<DynamicSource>();
        }

        public Task ProvideItems(IBundleBuildContext context, IReadOnlyList<IBundleItemTransform> itemTransforms, Action<IBundleSourceBuildItem> processor)
        {
            _logger.LogInformation($"Dynamic source is being evaluated. Params:{Environment.NewLine}{{PARAMS}}",
                context.Params != null ? string.Join(Environment.NewLine, context.Params.Select(p => $"{p.Key}={p.Value}")) : "No params.");

            var color =
                context.Params != null && context.Params.TryGetValue("c", out StringValues values) && values.Count > 0 && !string.IsNullOrEmpty(values[0]) ?
                values[0] :
                "000";

            // we are providing a dynamic input item that depends on the value of the 'c' query string parameter
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
