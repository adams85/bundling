using Microsoft.AspNetCore.Http;

namespace Karambolo.AspNetCore.Bundling.Internal.Helpers
{
    static class HttpContextStatic
    {
        static IHttpContextAccessor accessor;

        public static HttpContext Current => accessor?.HttpContext;

        public static void Initialize(IHttpContextAccessor httpContextAccessor)
        {
            accessor = httpContextAccessor;
        }
    }
}
