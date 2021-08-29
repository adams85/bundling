using System;
using System.Threading.Tasks;
using Karambolo.AspNetCore.Bundling.Internal;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Karambolo.AspNetCore.Bundling
{
#if !NETCOREAPP3_0_OR_GREATER
    using IWebHostEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;
#else
    using Microsoft.AspNetCore.Hosting;
#endif

    public class BundlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IBundleManager _bundleManager;
        private readonly StaticFileMiddleware _staticFileMiddleware;

        public BundlingMiddleware(RequestDelegate next, IWebHostEnvironment env, ILoggerFactory loggerFactory, IHttpContextAccessor httpContextAccessor,
            IOptions<BundleGlobalOptions> globalOptions, IBundleManagerFactory bundleManagerFactory, BundleCollection bundles, IOptions<BundlingOptions> options)
        {
            if (next == null)
                throw new ArgumentNullException(nameof(next));

            if (env == null)
                throw new ArgumentNullException(nameof(env));

            if (loggerFactory == null)
                throw new ArgumentNullException(nameof(loggerFactory));

            if (httpContextAccessor == null)
                throw new ArgumentNullException(nameof(httpContextAccessor));

            if (globalOptions == null)
                throw new ArgumentNullException(nameof(globalOptions));

            if (bundleManagerFactory == null)
                throw new ArgumentNullException(nameof(bundleManagerFactory));

            if (bundles == null)
                throw new ArgumentNullException(nameof(bundles));

            if (options == null)
                throw new ArgumentNullException(nameof(options));

            _next = next;

            BundleGlobalOptions globalOptionsUnwrapped = globalOptions.Value;
            BundlingOptions optionsUnwrapped = options.Value;

            _bundleManager = optionsUnwrapped.BundleManager ?? bundleManagerFactory.Create(bundles, new BundlingContext
            {
                BundlesPathPrefix = optionsUnwrapped.RequestPath,
                StaticFilesPathPrefix = optionsUnwrapped.StaticFilesRequestPath
            });

            var staticFileOptions = new StaticFileOptions();

            optionsUnwrapped.CopyTo(staticFileOptions);

            staticFileOptions.FileProvider = staticFileOptions.FileProvider ?? new BundleFileProvider(_bundleManager, httpContextAccessor);

            if (globalOptionsUnwrapped.EnableCacheHeader)
            {
                Action<StaticFileResponseContext> originalPrepareResponse = staticFileOptions.OnPrepareResponse;
                staticFileOptions.OnPrepareResponse = ctx =>
                {
                    Microsoft.AspNetCore.Http.Headers.ResponseHeaders headers = ctx.Context.Response.GetTypedHeaders();
                    headers.CacheControl = new CacheControlHeaderValue { MaxAge = globalOptionsUnwrapped.CacheHeaderMaxAge };
                    originalPrepareResponse?.Invoke(ctx);
                };
            }

            _staticFileMiddleware = new StaticFileMiddleware(next, env, Options.Create(staticFileOptions), loggerFactory);
        }

        public async Task Invoke(HttpContext context)
        {
            if (await _bundleManager.TryEnsureUrlAsync(context))
                await _staticFileMiddleware.Invoke(context);
            else
                await _next(context);
        }
    }
}
