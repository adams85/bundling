using System;
using Microsoft.Extensions.Primitives;

namespace Karambolo.AspNetCore.Bundling.Internal.Helpers
{
    public abstract class ChangeTokenObserver : IDisposable
    {
        private readonly object _gate = new object();
        private Func<IChangeToken> _changeTokenFactory;
        private IDisposable _changeTokenReleaser;
        private bool _isDisposed;

        protected virtual void DisposeCore() { }

        public void Dispose()
        {
            lock (_gate)
            {
                if (_isDisposed)
                    return;

                _isDisposed = true;
                _changeTokenFactory = null;
                ResetChangeSourceCore();
            }

            DisposeCore();
        }

        private void ResetChangeSourceCore()
        {
            if (_changeTokenReleaser != null)
            {
                _changeTokenReleaser.Dispose();
                _changeTokenReleaser = null;
            }

            if (_changeTokenFactory != null)
            {
                IChangeToken changeToken = _changeTokenFactory();
                _changeTokenReleaser = changeToken.RegisterChangeCallback(OnChanged, null);
            }
        }

        protected void ResetChangeSource(Func<IChangeToken> changeTokenFactory)
        {
            if (changeTokenFactory == null)
                throw new ArgumentNullException(nameof(changeTokenFactory));

            lock (_gate)
            {
                _changeTokenFactory = changeTokenFactory;
                ResetChangeSourceCore();
            }
        }

        private void OnChanged(object state)
        {
            lock (_gate)
            {
                if (_isDisposed)
                    return;

                ResetChangeSourceCore();
            }

            OnChanged();
        }

        protected abstract void OnChanged();
    }
}
