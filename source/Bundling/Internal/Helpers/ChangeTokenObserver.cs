using System;
using System.Threading;
using Microsoft.Extensions.Primitives;

namespace Karambolo.AspNetCore.Bundling.Internal.Helpers
{
    public abstract class ChangeTokenObserver : IDisposable
    {
        private Func<IChangeToken> _changeTokenFactory;
        private IDisposable _changeTokenReleaser;

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

        private IDisposable AcquireChangeToken()
        {
            IChangeToken changeToken = _changeTokenFactory();
            IDisposable result = Interlocked.CompareExchange(ref _changeTokenReleaser, changeToken.RegisterChangeCallback(OnChanged, null), NullDisposable.Instance);
            if (result == null)
                _changeTokenReleaser.Dispose();
            return result;
        }

        private IDisposable ReleaseChangeToken(bool dispose)
        {
            IDisposable result = Interlocked.Exchange(ref _changeTokenReleaser, dispose ? null : NullDisposable.Instance);
            result?.Dispose();
            return result;
        }

        private void OnChanged(object state)
        {
            ReleaseChangeToken(dispose: false);
            if (AcquireChangeToken() != null)
                OnChanged();
        }

        protected abstract void OnChanged();
    }
}
