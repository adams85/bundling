using System;
using System.Threading;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace DynamicBundle
{
    public class DynamicSourceInvalidator : IDisposable
    {
        readonly object _gate = new object();
        CancellationTokenSource _cts;
        bool _isDisposed;

        public void Dispose()
        {
            lock (_gate)
            {
                _isDisposed = true;
                _cts?.Dispose();
                _cts = null;
            }
        }

        public IChangeToken CreateChangeToken()
        {
            CancellationTokenSource originalCts, newCts;

            lock (_gate)
                if (!_isDisposed)
                {
                    originalCts = _cts;
                    _cts = newCts = new CancellationTokenSource();
                }
                else
                    return NullChangeToken.Singleton;

            originalCts?.Dispose();

            return new CancellationChangeToken(newCts.Token);
        }

        public void Invalidate()
        {
            CancellationTokenSource originalCts;

            lock (_gate)
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(string.Empty);

                originalCts = _cts;
                _cts = new CancellationTokenSource();
            }

            if (originalCts != null)
            {
                originalCts.Cancel();
                originalCts.Dispose();
            }
        }
    }
}
