using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Karambolo.AspNetCore.Bundling.Internal;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace CustomBundleManager
{
    public delegate string BundleFileToUrlMapper(IFileProvider fileProvider, string filePath, IUrlHelper urlHelper, IWebHostEnvironment environment);

    public static class HtmlHelperExtensions
    {
        private static readonly Uri s_dummyBaseUri = new Uri("xxx://");

        public static async Task<string[]> GetIndividualBundleFileUrlsAsync(this IHtmlHelper htmlHelper, string bundlePath, BundleFileToUrlMapper mapFileToUrl = null)
        {
            var httpContext = htmlHelper.ViewContext.HttpContext;

            var urlHelperFactory = httpContext.RequestServices.GetRequiredService<IUrlHelperFactory>();
            var urlHelper = urlHelperFactory.GetUrlHelper(htmlHelper.ViewContext);

            var bundleUri = new Uri(s_dummyBaseUri, urlHelper.Content(bundlePath));
            UriHelper.FromAbsolute(bundleUri.ToString(), out _, out _, out var path, out var query, out _);

            var bundleManagerFactory = httpContext.RequestServices.GetRequiredService<IBundleManagerFactory>();

            (IFileProvider FileProvider, string FilePath)[] files = null;

            for (int i = 0, n = bundleManagerFactory.Instances.Count; i < n; i++)
                if (bundleManagerFactory.Instances[i] is CustomBundleManager customBundleManager)
                    if ((files = await customBundleManager.TryGetInputFilesAsync(path, query, httpContext)) != null)
                        break;

            if (files == null)
                return Array.Empty<string>();

            var webHostEnvironment = httpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
            mapFileToUrl ??= DefaultMapFileToUrl;

            return files
                .Select(file => mapFileToUrl(file.FileProvider, file.FilePath, urlHelper, webHostEnvironment))
                .Where(url => url != null)
                .ToArray();

            static string DefaultMapFileToUrl(IFileProvider fileProvider, string filePath, IUrlHelper urlHelper, IWebHostEnvironment env) =>
                ReferenceEquals(fileProvider, env.WebRootFileProvider) ? urlHelper.Content(filePath) : null;
        }

        private static async Task<IHtmlContent> RenderIndividualBundleFilesAsync(this IHtmlHelper htmlHelper, string bundlePath, string tagFormat, BundleFileToUrlMapper mapFileToUrl)
        {
            var urls = await htmlHelper.GetIndividualBundleFileUrlsAsync(bundlePath, mapFileToUrl);

            var sb = new StringBuilder();
            for (int i = 0; i < urls.Length; i++)
            {
                sb.AppendFormat(tagFormat, urls[i]);
                sb.AppendLine();
            }
            return new HtmlString(sb.ToString());
        }

        public static Task<IHtmlContent> RenderIndividualBundleScriptsAsync(this IHtmlHelper htmlHelper, string bundlePath, BundleFileToUrlMapper mapFileToUrl = null)
        {
            return htmlHelper.RenderIndividualBundleFilesAsync(bundlePath, "<script src=\"{0}\"></script>", mapFileToUrl);
        }

        public static Task<IHtmlContent> RenderIndividualBundleStylesAsync(this IHtmlHelper htmlHelper, string bundlePath, BundleFileToUrlMapper mapFileToUrl = null)
        {
            return htmlHelper.RenderIndividualBundleFilesAsync(bundlePath, "<link href=\"{0}\" rel=\"stylesheet\"/>", mapFileToUrl);
        }
    }
}
