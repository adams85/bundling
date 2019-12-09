using System;
using System.Collections.Generic;
using System.Threading;

namespace Karambolo.AspNetCore.Bundling.Internal.Helpers
{
#if NETSTANDARD2_0
    using IHostApplicationLifetime = Microsoft.AspNetCore.Hosting.IApplicationLifetime;
#else
    using Microsoft.Extensions.Hosting;
#endif

    internal class NullDisposable : IDisposable
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

    public interface IScopedDisposer : IDisposable
    {
        void Register(IDisposable disposable);
    }

    public class DefaultScopedDisposer : IScopedDisposer
    {
        private bool _isDisposed;
        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        public void Register(IDisposable disposable)
        {
            if (disposable == null)
                throw new ArgumentNullException(nameof(disposable));

            if (_isDisposed)
                throw new ObjectDisposedException(nameof(DefaultScopedDisposer));

            _disposables.Add(disposable);
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _disposables.ForEach(d => d.Dispose());
                _isDisposed = true;
            }
        }
    }

    public static class DisposeHelper
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

        public static T ScheduleDisposeForScopeEnd<T>(this IScopedDisposer disposer, T disposable)
            where T : IDisposable
        {
            if (disposer == null)
                throw new ArgumentNullException(nameof(disposer));

            disposer.Register(disposable);
            return disposable;
        }
    }
}
