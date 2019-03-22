using Microsoft.AspNetCore.Http;

namespace Karambolo.AspNetCore.Bundling.Internal.Helpers
{
    internal static class HttpContextStatic
    {
        private static IHttpContextAccessor s_accessor;

        public static HttpContext Current => s_accessor?.HttpContext;

        public static void Initialize(IHttpContextAccessor httpContextAccessor)
        {
            s_accessor = httpContextAccessor;
        }
    }
}
