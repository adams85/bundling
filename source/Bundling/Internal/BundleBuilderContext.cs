using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Karambolo.AspNetCore.Bundling.Internal
{
    public class BundleBuilderContext : IBundleBuilderContext
    {
        public IBundlingContext BundlingContext { get; set; }
        public PathString AppBasePath { get; set; }
        public IDictionary<string, StringValues> Params { get; set; }
        public IBundleModel Bundle { get; set; }
        public CancellationToken CancellationToken { get; set; }

        /// <remarks>
        /// Not null when change detection is enabled, otherwise null.
        /// </remarks>
        public ISet<IChangeSource> ChangeSources { get; set; }

        public string Result { get; set; }

        public IDisposable UseExternalCancellationToken(CancellationToken cancellationToken)
        {
            return new ExternalCancellationTokenScope(this, cancellationToken);
        }

        private sealed class ExternalCancellationTokenScope : IDisposable
        {
            private BundleBuilderContext _context;
            private readonly CancellationToken _originalCancellationToken;
            private readonly CancellationTokenSource _linkedCts;

            public ExternalCancellationTokenScope(BundleBuilderContext context, CancellationToken externalCancellationToken)
            {
                _context = context;
                _originalCancellationToken = context.CancellationToken;
                _linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_originalCancellationToken, externalCancellationToken);
                context.CancellationToken = _linkedCts.Token;
            }

            public void Dispose()
            {
                BundleBuilderContext context = Interlocked.Exchange(ref _context, null);
                if (context != null)
                {
                    _linkedCts.Dispose();
                    context.CancellationToken = _originalCancellationToken;
                }
            }
        }
    }
}
