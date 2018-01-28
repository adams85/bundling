using System;
using System.Threading;
using Microsoft.Extensions.Primitives;

namespace Karambolo.AspNetCore.Bundling.Internal.Helpers
{
    public abstract class ChangeTokenObserver : IDisposable
    {
        Func<IChangeToken> _changeTokenFactory;
        IDisposable _changeTokenReleaser;

        protected virtual void DisposeCore() { }

        public void Dispose()
        {
            ReleaseChangeToken(dispose: true);
            DisposeCore();
        }

        protected void Initialize(Func<IChangeToken> changeTokenFactory)
        {
            _changeTokenFactory = changeTokenFactory;
            _changeTokenReleaser = NullDisposable.Instance;

            AcquireChangeToken();
        }

        IDisposable AcquireChangeToken()
        {
            var changeToken = _changeTokenFactory();
            var result = Interlocked.CompareExchange(ref _changeTokenReleaser, changeToken.RegisterChangeCallback(OnChanged, null), NullDisposable.Instance);
            if (result == null)
                _changeTokenReleaser.Dispose();
            return result;
        }

        IDisposable ReleaseChangeToken(bool dispose)
        {
            var result = Interlocked.Exchange(ref _changeTokenReleaser, dispose ? null : NullDisposable.Instance);
            result?.Dispose();
            return result;
        }

        void OnChanged(object state)
        {
            ReleaseChangeToken(dispose: false);
            if (AcquireChangeToken() != null)
                OnChanged();
        }

        protected abstract void OnChanged();
    }
}
