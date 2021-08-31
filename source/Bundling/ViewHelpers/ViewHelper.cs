using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Karambolo.AspNetCore.Bundling.Internal;
using Karambolo.AspNetCore.Bundling.Internal.Helpers;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace Karambolo.AspNetCore.Bundling.ViewHelpers
{
    internal static class ViewHelper
    {
        internal static readonly StaticFileUrlToFileMapper NullUrlToFileMapper =
            delegate (string url, IUrlHelper urlHelper, out IFileProvider fileProvider, out string filePath, out bool caseSensitiveFilePaths)
            {
                (fileProvider, filePath, caseSensitiveFilePaths) = (default, default, default);
                return false;
            };

        private static readonly RouteData s_dummyRouteData = new RouteData();
        private static readonly ActionDescriptor s_dummyActionDescriptor = new ActionDescriptor();

        private static IUrlHelper CreateUrlHelperFrom(HttpContext httpContext)
        {
            var actionContext = new ActionContext(httpContext, s_dummyRouteData, s_dummyActionDescriptor);
            return new UrlHelper(actionContext);
        }

        internal static bool TryGetBundle(HttpContext httpContext, IBundleManagerFactory bundleManagerFactory, string absolutePath,
            out QueryString query, out IBundleManager bundleManager, out IBundleModel bundle)
        {
            UrlUtils.DeconstructPath(absolutePath, out PathString path, out query, out _);

            for (int i = 0, n = bundleManagerFactory.Instances.Count; i < n; i++)
                if ((bundleManager = bundleManagerFactory.Instances[i]).TryGetBundle(httpContext, path, out bundle))
                    return true;

            bundleManager = default;
            bundle = default;
            return false;
        }

        internal static string AdjustStaticFileUrl(IUrlHelper urlHelper, string url, bool addVersion, StaticFileUrlToFileMapper urlToFileMapper)
        {
            if (addVersion)
            {
                IStaticFileUrlHelper staticFileUrlHelper = urlHelper.ActionContext.HttpContext.RequestServices.GetRequiredService<IStaticFileUrlHelper>();
                return staticFileUrlHelper.AddVersion(url, urlHelper, urlToFileMapper);
            }

            return url;
        }

        private static Task<string> GenerateUrlCoreAsync(IUrlHelper urlHelper, IBundleManagerFactory bundleManagerFactory, string path, bool? addVersion)
        {
            bool actualAddVersion = addVersion ?? bundleManagerFactory.GlobalOptions.Value.EnableCacheBusting;
            StaticFileUrlToFileMapper urlToFileMapper = bundleManagerFactory.GlobalOptions.Value.StaticFileUrlToFileMapper ?? NullUrlToFileMapper;

            path = urlHelper.Content(path);
            HttpContext httpContext = urlHelper.ActionContext.HttpContext;
            return
                TryGetBundle(httpContext, bundleManagerFactory, path, out QueryString query, out IBundleManager bundleManager, out IBundleModel bundle) ?
                bundleManager.GenerateUrlAsync(httpContext, bundle, query, actualAddVersion) :
                Task.FromResult(AdjustStaticFileUrl(urlHelper, path, actualAddVersion, urlToFileMapper));
        }

        public static async Task<string> UrlAsync(string path, bool? addVersion)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            HttpContext httpContext = HttpContextStatic.Current;
            if (httpContext == null)
                throw ErrorHelper.HttpContextNotAvailable();

            IBundleManagerFactory bundleManagerFactory = httpContext.RequestServices.GetRequiredService<IBundleManagerFactory>();
            IUrlHelper urlHelper = CreateUrlHelperFrom(httpContext);

            return await GenerateUrlCoreAsync(urlHelper, bundleManagerFactory, path, addVersion);
        }

        private static Task<IHtmlContent> RenderFormatCoreAsync(IUrlHelper urlHelper, IBundleManagerFactory bundleManagerFactory, string path, string tagFormat, bool? addVersion)
        {
            bool actualAddVersion = addVersion ?? bundleManagerFactory.GlobalOptions.Value.EnableCacheBusting;
            StaticFileUrlToFileMapper urlToFileMapper = bundleManagerFactory.GlobalOptions.Value.StaticFileUrlToFileMapper ?? NullUrlToFileMapper;

            path = urlHelper.Content(path);
            HttpContext httpContext = urlHelper.ActionContext.HttpContext;
            return
                TryGetBundle(httpContext, bundleManagerFactory, path, out QueryString query, out IBundleManager bundleManager, out IBundleModel bundle) ?
                bundle.HtmlRenderer.RenderHtmlAsync(urlHelper, bundleManager, bundle, query, tagFormat, actualAddVersion, urlToFileMapper) :
                Task.FromResult<IHtmlContent>(new HtmlFormattableString(tagFormat, AdjustStaticFileUrl(urlHelper, path, actualAddVersion, urlToFileMapper)));
        }

        public static async Task<IHtmlContent> RenderFormatAsync(string tagFormat, bool? addVersion, params string[] paths)
        {
            if (paths == null)
                throw new ArgumentNullException(nameof(paths));

            if (Array.FindIndex(paths, p => p == null) >= 0)
                throw ErrorHelper.ArrayCannotContainNull(nameof(paths));

            HttpContext httpContext = HttpContextStatic.Current;
            if (httpContext == null)
                throw ErrorHelper.HttpContextNotAvailable();

            IBundleManagerFactory bundleManagerFactory = httpContext.RequestServices.GetRequiredService<IBundleManagerFactory>();
            IUrlHelper urlHelper = CreateUrlHelperFrom(httpContext);

            if (paths.Length > 1)
            {
                var renderTasks = new List<Task<IHtmlContent>>(paths.Length);

                foreach (string path in paths)
                    renderTasks.Add(RenderFormatCoreAsync(urlHelper, bundleManagerFactory, path, tagFormat, addVersion));

                await Task.WhenAll(renderTasks);

                var builder = new HtmlContentBuilder(renderTasks.Count * 2 - 1);
                builder.AppendHtml(renderTasks[0].Result);

                for (int i = 1, n = renderTasks.Count; i < n; i++)
                {
                    builder.AppendLine();
                    builder.AppendHtml(renderTasks[i].Result);
                }

                return builder;
            }
            else if (paths.Length > 0)
                return await RenderFormatCoreAsync(urlHelper, bundleManagerFactory, paths[0], tagFormat, addVersion);
            else
                return HtmlString.Empty;
        }
    }
}
