using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Karambolo.AspNetCore.Bundling.Internal.Helpers;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Karambolo.AspNetCore.Bundling.Internal.Models
{
    public abstract class BundleSourceModelBase : ChangeTokenObserver, IBundleSourceModel
    {
        private static readonly Action<IBundleItemTransformContext> s_onProcessedNoop = _ => { };

        private readonly Action<IBundleItemTransformContext> _onProcessed;
        private List<string> _filesToWatch;

        protected BundleSourceModelBase(bool enableChangeDetection)
        {
            _onProcessed = enableChangeDetection ? ResetChangeDetection : s_onProcessedNoop;
        }

        protected virtual IEnumerable<IChangeToken> GetChangeTokens(IBundleItemTransformContext context, List<string> filesToWatch)
        {
            return
                context is IFileBundleItemTransformContext fileItemContext ?
                filesToWatch.Select(file => fileItemContext.FileProvider.Watch(file)) :
                Enumerable.Empty<IChangeToken>();
        }

        private void ResetChangeDetection(IBundleItemTransformContext context)
        {
            if (context is IFileBundleItemTransformContext fileItemContext && fileItemContext.AdditionalSourceFilePaths != null)
                _filesToWatch.AddRange(fileItemContext.AdditionalSourceFilePaths);

            ResetChangeSource(() =>
            {
                IChangeToken[] changeTokens = GetChangeTokens(context, _filesToWatch).ToArray();
                return 
                    changeTokens.Length > 1 ? new CompositeChangeToken(changeTokens) : 
                    changeTokens.Length == 1 ? changeTokens[0] :
                    NullChangeToken.Singleton;
            });
        }

        public void OnProcessed(IBundleItemTransformContext context)
        {
            _onProcessed(context);
        }

        public event EventHandler Changed;

        protected override void OnChanged()
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }

        protected abstract Task ProvideBuildItemsCoreAsync(IBundleBuildContext context, Action<IBundleSourceBuildItem> processor, List<string> filesToWatch);

        public Task ProvideBuildItemsAsync(IBundleBuildContext context, Action<IBundleSourceBuildItem> processor)
        {
            if (_onProcessed != s_onProcessedNoop)
                _filesToWatch = new List<string>();

            return ProvideBuildItemsCoreAsync(context, processor, _filesToWatch);
        }
    }
}
