using System;
using System.Collections.Generic;
using System.Text;
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

        private static async Task<string> GenerateUrlCoreAsync(string pathString, IBundleManagerFactory bundleManagerFactory, HttpContext httpContext)
        {
            var actionContext = new ActionContext(httpContext, s_dummyRouteData, s_dummyActionDescriptor);
            var urlHelper = new UrlHelper(actionContext);
            UrlUtils.FromRelative(urlHelper.Content(pathString), out PathString path, out QueryString query, out FragmentString fragment);

            string url = null;
            var n = bundleManagerFactory.Instances.Count;
            for (var i = 0; i < n; i++)
                if ((url = await bundleManagerFactory.Instances[i].TryGenerateUrlAsync(path, query, httpContext)) != null)
                    break;

            return url ?? pathString;
        }

        public static async Task<IHtmlContent> UrlAsync(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            HttpContext httpContext = HttpContextStatic.Current;
            if (httpContext == null)
                throw ErrorHelper.HttpContextNotAvailable();

            IBundleManagerFactory bundleManagerFactory = httpContext.RequestServices.GetRequiredService<IBundleManagerFactory>();

            var result = await GenerateUrlCoreAsync(path, bundleManagerFactory, httpContext);
            return new HtmlString(result);
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

            var generateTasks = new List<Task<string>>();
            foreach (var path in paths)
                generateTasks.Add(GenerateUrlCoreAsync(path, bundleManagerFactory, httpContext));

            await Task.WhenAll(generateTasks);

            var sb = new StringBuilder();
            var n = generateTasks.Count;
            for (var i = 0; i < n; i++)
            {
                sb.AppendFormat(tagFormat, generateTasks[i].Result);
                sb.AppendLine();
            }
            return new HtmlString(sb.ToString());
        }
    }
}
