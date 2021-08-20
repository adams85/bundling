using System;
using System.Threading;
using Microsoft.AspNetCore.Http;

namespace Karambolo.AspNetCore.Bundling.Internal.Helpers
{
#if NETSTANDARD2_0
    using IHostApplicationLifetime = Microsoft.AspNetCore.Hosting.IApplicationLifetime;
#else
    using Microsoft.Extensions.Hosting;
#endif

    internal sealed class NullDisposable : IDisposable
    {
        public static readonly NullDisposable Instance = new NullDisposable();

        private NullDisposable() { }

        public void Dispose() { }
    }

    internal readonly struct CompositeDisposable : IDisposable
    {
        private readonly IDisposable _disposable1;
        private readonly IDisposable _disposable2;

        public CompositeDisposable(IDisposable disposable1, IDisposable disposable2)
        {
            _disposable1 = disposable1;
            _disposable2 = disposable2;
        }

        public void Dispose()
        {
            _disposable1?.Dispose();
            _disposable2?.Dispose();
        }
    }

    internal static class DisposeHelper
    {
        public static T ScheduleDisposeForShutdown<T>(this IHostApplicationLifetime appLifetime, T disposable)
            where T : IDisposable
        {
            if (appLifetime == null)
                throw new ArgumentNullException(nameof(appLifetime));

            var registration = default(CancellationTokenRegistration);
            registration = appLifetime.ApplicationStopping.Register(() =>
            {
                disposable.Dispose();
                registration.Dispose();
            });
            return disposable;
        }

        public static T ScheduleDisposeForRequestEnd<T>(this HttpContext httpContext, T disposable)
            where T : IDisposable
        {
            if (httpContext == null)
                throw new ArgumentNullException(nameof(httpContext));

            httpContext.Response.RegisterForDispose(disposable);
            return disposable;
        }
    }
}
