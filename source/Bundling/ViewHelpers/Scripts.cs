using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;

namespace Karambolo.AspNetCore.Bundling.ViewHelpers
{
    public static class Scripts
    {
        public static string DefaultTagFormat { get; set; } = "<script src=\"{0}\"></script>";

        public static Task<IHtmlContent> UrlAsync(string path)
        {
            return ViewHelper.UrlAsync(path);
        }

        public static Task<IHtmlContent> RenderAsync(params string[] paths)
        {
            return ViewHelper.RenderFormatAsync(DefaultTagFormat, paths);
        }

        public static Task<IHtmlContent> RenderFormatAsync(string tagFormat, params string[] paths)
        {
            return ViewHelper.RenderFormatAsync(tagFormat, paths);
        }
    }
}
