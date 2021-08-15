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

namespace Karambolo.AspNetCore.Bundling.ViewHelpers
{
    internal static class ViewHelper
    {
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
            UrlUtils.FromRelative(absolutePath, out PathString path, out query, out _);

            for (int i = 0, n = bundleManagerFactory.Instances.Count; i < n; i++)
                if ((bundleManager = bundleManagerFactory.Instances[i]).TryGetBundle(httpContext, path, out bundle))
                    return true;

            bundleManager = default;
            bundle = default;
            return false;
        }

        private static Task<string> GenerateUrlCoreAsync(HttpContext httpContext, IBundleManagerFactory bundleManagerFactory, string absolutePath)
        {
            return
                TryGetBundle(httpContext, bundleManagerFactory, absolutePath, out QueryString query, out IBundleManager bundleManager, out IBundleModel bundle) ?
                bundleManager.GenerateUrlAsync(httpContext, bundle, query) :
                Task.FromResult(absolutePath);
        }

        public static async Task<IHtmlContent> UrlAsync(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            HttpContext httpContext = HttpContextStatic.Current;
            if (httpContext == null)
                throw ErrorHelper.HttpContextNotAvailable();

            IBundleManagerFactory bundleManagerFactory = httpContext.RequestServices.GetRequiredService<IBundleManagerFactory>();
            IUrlHelper urlHelper = CreateUrlHelperFrom(httpContext);

            string url = await GenerateUrlCoreAsync(httpContext, bundleManagerFactory, urlHelper.Content(path));
            
            return new HtmlString(url);
        }

        private static Task<IHtmlContent> RenderFormatCoreAsync(IUrlHelper urlHelper, IBundleManagerFactory bundleManagerFactory, string absolutePath, string tagFormat)
        {
            return
                TryGetBundle(urlHelper.ActionContext.HttpContext, bundleManagerFactory, absolutePath, out QueryString query, out IBundleManager bundleManager, out IBundleModel bundle) ?
                bundle.HtmlRenderer.RenderHtmlAsync(urlHelper, bundleManager, bundle, query, tagFormat) :
                Task.FromResult<IHtmlContent>(new HtmlString(string.Format(tagFormat, absolutePath)));
        }

        public static async Task<IHtmlContent> RenderFormatAsync(string tagFormat, params string[] paths)
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
                    renderTasks.Add(RenderFormatCoreAsync(urlHelper, bundleManagerFactory, urlHelper.Content(path), tagFormat));

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
                return await RenderFormatCoreAsync(urlHelper, bundleManagerFactory, urlHelper.Content(paths[0]), tagFormat);
            else
                return HtmlString.Empty;
        }
    }
}
