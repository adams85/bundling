using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Karambolo.AspNetCore.Bundling.Internal
{
    public class DefaultBundleBuilder : IBundleBuilder
    {
        protected virtual async Task<IBundleItemTransformContext> ApplyItemTransformsAsync(IBundleItemTransformContext context, IReadOnlyList<IBundleItemTransform> transforms)
        {
            if (transforms != null)
                for (int i = 0, n = transforms.Count; i < n; i++)
                {
                    context.BuildContext.CancellationToken.ThrowIfCancellationRequested();

                    IBundleItemTransform transform = transforms[i];
                    await transform.TransformAsync(context);
                    transform.Transform(context);
                }

            return context;
        }

        protected virtual async Task AggregateAsync(IBundleTransformContext context, IReadOnlyList<IBundleTransform> transforms)
        {
            if (transforms != null)
                for (int i = 0, n = transforms.Count; i < n; i++)
                    if (transforms[i] is IAggregatorBundleTransform aggregatorTransform)
                    {
                        context.BuildContext.CancellationToken.ThrowIfCancellationRequested();

                        await aggregatorTransform.AggregateAsync(context);
                        if (context.Content != null)
                            return;

                        aggregatorTransform.Aggregate(context);
                        if (context.Content != null)
                            return;
                    }

            context.BuildContext.CancellationToken.ThrowIfCancellationRequested();

            // falling back to simple concatenation when aggregation was not handled by the transforms
            context.Content = string.Join(context.BuildContext.Bundle.ConcatenationToken, context.TransformedItemContexts.Select(itemContext => itemContext.Content));
        }

        protected virtual async Task<string> ApplyTransformsAsync(IBundleTransformContext context, IReadOnlyList<IBundleTransform> transforms)
        {
            if (transforms != null)
                for (int i = 0, n = transforms.Count; i < n; i++)
                {
                    context.BuildContext.CancellationToken.ThrowIfCancellationRequested();

                    IBundleTransform transform = transforms[i];
                    await transform.TransformAsync(context);
                    transform.Transform(context);
                }

            return context.Content;
        }

        public virtual async Task BuildAsync(IBundleBuilderContext context)
        {
            // applying transforms to items

            var itemTransformTasks = new ConcurrentQueue<Task<IBundleItemTransformContext>>();

            CancellationToken cancellationToken = context.CancellationToken;
            using (var errorCts = new CancellationTokenSource())
            using (context.UseExternalCancellationToken(errorCts.Token))
            {
                for (int i = 0, n = context.Bundle.Sources.Length; i < n; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    IBundleSourceModel source = context.Bundle.Sources[i];
                    await source.ProvideBuildItemsAsync(context, it => itemTransformTasks.Enqueue(Task.Run(async () =>
                    {
                        try { return await ApplyItemTransformsAsync(it.ItemTransformContext, it.ItemTransforms); }
                        catch (Exception ex) when (!(ex is OperationCanceledException))
                        {
                            errorCts.Cancel(); // stop processing other enqueued items
                            throw;
                        }
                    }, cancellationToken)));
                }

                // "If any of the supplied tasks completes in a faulted state, the returned task will also complete in a Faulted state, where its exceptions will contain the aggregation of the set of unwrapped exceptions from each of the supplied tasks."
                // "If none of the supplied tasks faulted but at least one of them was canceled, the returned task will end in the Canceled state."
                // https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.task.whenall?view=netcore-3.1
                await Task.WhenAll(itemTransformTasks);
            }

            // aggregating items

            var transformContext = new BundleTransformContext(context)
            {
                TransformedItemContexts = itemTransformTasks.Select(task => task.GetAwaiter().GetResult()).ToArray()
            };

            itemTransformTasks = null;

            await AggregateAsync(transformContext, context.Bundle.Transforms);

            // applying transforms to bundle

            transformContext.TransformedItemContexts = null;

            context.Result = await ApplyTransformsAsync(transformContext, context.Bundle.Transforms);
        }
    }
}
