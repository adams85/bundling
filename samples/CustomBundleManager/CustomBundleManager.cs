using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Karambolo.AspNetCore.Bundling;
using Karambolo.AspNetCore.Bundling.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CustomBundleManager
{
    public class CustomBundleManager : BundleManager
    {
        private readonly Dictionary<PathString, IBundleModel> _bundles = new Dictionary<PathString, IBundleModel>();
        private readonly IBundlingContext _bundlingContext;

        public CustomBundleManager(int id, BundleCollection bundles, IBundlingContext bundlingContext, CancellationToken shutdownToken,
            IEnumerable<IBundleModelFactory> modelFactories, IBundleCache cache, IBundleVersionProvider versionProvider, IBundleUrlHelper urlHelper,
            ILoggerFactory loggerFactory, ISystemClock clock, IOptions<BundleGlobalOptions> globalOptions)
            : base(id, bundles, bundlingContext, shutdownToken, modelFactories, cache, versionProvider, urlHelper, loggerFactory, clock, globalOptions)
        {
            _bundlingContext = bundlingContext;
        }

        // HACK: We need the _bundles dictionary of the base class but unfortunately it's private, so we resort to this little trick for now.
        protected override IBundleModel CreateModel(Bundle bundle)
        {
            var model = base.CreateModel(bundle);
            _bundles.Add(bundle.Path, model);
            return model;
        }

        // This method collects the input file items of the referenced bundle without actually building it.
        // TODO: There's a gotcha though: ProvideBuildItemsAsync also pre-loads the files' content, which we cannot prevent currently.
        // This is far from optimal but this feature is only used in development environments so it may be accaptable. However, this should be fixed in a future version!
        public async Task<(IFileProvider FileProvider, string FilePath)[]> TryGetInputFilesAsync(PathString path, QueryString query, HttpContext httpContext)
        {
            PathString pathPrefix = httpContext.Request.PathBase + _bundlingContext.BundlesPathPrefix;

            if (!path.StartsWithSegments(pathPrefix, out PathString bundlePath) ||
                !_bundles.TryGetValue(bundlePath, out IBundleModel bundle))
                return null;

            var context = new BundleBuilderContext
            {
                BundlingContext = _bundlingContext,
                AppBasePath = httpContext.Request.PathBase,
                Params = null,
                Bundle = bundle,
                ChangeSources = null,
                CancellationToken = httpContext.RequestAborted
            };

            var items = new ConcurrentQueue<IFileBundleItemTransformContext>();

            for (int i = 0, n = context.Bundle.Sources.Length; i < n; i++)
            {
                IBundleSourceModel source = context.Bundle.Sources[i];
                await source.ProvideBuildItemsAsync(context, it =>
                {
                    if (it is IFileBundleItemTransformContext fileItem)
                        items.Enqueue(fileItem);
                });
            }

            return items.Select(it => (it.FileProvider, it.FilePath)).ToArray();
        }
    }
}
