using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNetCore.Hosting;

namespace Karambolo.AspNetCore.Bundling.Internal.Helpers
{
    class NullDisposable : IDisposable
    {
        public static readonly NullDisposable Instance = new NullDisposable();

        NullDisposable() { }

        public void Dispose() { }
    }

    struct CompositeDisposable : IDisposable
    {
        readonly IDisposable _disposable1;
        readonly IDisposable _disposable2;

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
        bool isDisposed;
        List<IDisposable> _disposables = new List<IDisposable>();

        public void Register(IDisposable disposable)
        {
            if (isDisposed)
                throw new ObjectDisposedException(nameof(DefaultScopedDisposer));

            _disposables.Add(disposable);
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                _disposables.ForEach(d => d.Dispose());
                isDisposed = true;
            }
        }
    }

    public static class DisposeHelper
    {
        public static T ScheduleDisposeForShutdown<T>(this IApplicationLifetime @this, T disposable)
            where T : IDisposable
        {
            var registration = default(CancellationTokenRegistration);
            registration = @this.ApplicationStopping.Register(() =>
            {
                disposable.Dispose();
                registration.Dispose();
            });
            return disposable;
        }

        public static T ScheduleDisposeForScopeEnd<T>(this IScopedDisposer @this, T disposable)
            where T : IDisposable
        {
            @this.Register(disposable);
            return disposable;
        }
    }
}
