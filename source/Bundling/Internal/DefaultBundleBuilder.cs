using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Karambolo.AspNetCore.Bundling.Internal
{
    public class DefaultBundleBuilder : IBundleBuilder
    {
        protected virtual void CreateItemTransformPipeline(IBundleBuilderContext context, out ITargetBlock<IBundleSourceBuildItem> input, out ISourceBlock<IBundleItemTransformContext> output)
        {
            var transformBlock = new TransformBlock<IBundleSourceBuildItem, IBundleItemTransformContext>(
                it => ApplyItemTransformsAsync(it.ItemTransformContext, it.ItemTransforms),
                new ExecutionDataflowBlockOptions
                {
                    CancellationToken = context.CancellationToken,
                    EnsureOrdered = true,
                    MaxDegreeOfParallelism = DataflowBlockOptions.Unbounded,
                    SingleProducerConstrained = true,
                });

            input = transformBlock;
            output = transformBlock;
        }

        protected virtual async Task<IBundleItemTransformContext> ApplyItemTransformsAsync(IBundleItemTransformContext context, IReadOnlyList<IBundleItemTransform> transforms)
        {
            if (transforms != null)
            {
                var n = transforms.Count;
                for (var i = 0; i < n; i++)
                {
                    context.BuildContext.CancellationToken.ThrowIfCancellationRequested();

                    IBundleItemTransform transform = transforms[i];
                    await transform.TransformAsync(context);
                    transform.Transform(context);
                }
            }

            return context;
        }

        protected virtual async Task AggregateAsync(IBundleTransformContext context, IReadOnlyList<IBundleTransform> transforms)
        {
            if (transforms != null)
                for (int i = 0, n = transforms.Count; i < n; i++)
                    if (transforms[i] is IAggregatorBundleTransform aggregatorTransform)
                    {
                        await aggregatorTransform.AggregateAsync(context);
                        if (context.Content != null)
                            return;

                        aggregatorTransform.Aggregate(context);
                        if (context.Content != null)
                            return;
                    }

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
            // building items

            CreateItemTransformPipeline(context, out ITargetBlock<IBundleSourceBuildItem> input, out ISourceBlock<IBundleItemTransformContext> output);

            // consumer
            async Task<List<IBundleItemTransformContext>> ConsumeAsync()
            {
                var itemContexts = new List<IBundleItemTransformContext>();

                while (await output.OutputAvailableAsync(context.CancellationToken))
                    itemContexts.Add(output.Receive());

                return itemContexts;
            };

            Task<List<IBundleItemTransformContext>> consumeTask = ConsumeAsync();

            // producer
            var n = context.Bundle.Sources.Length;
            for (var i = 0; i < n; i++)
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                IBundleSourceModel source = context.Bundle.Sources[i];
                await source.ProvideBuildItemsAsync(context, it => input.Post(it));
            }
            input.Complete();

            // building result

            var transformContext = new BundleTransformContext(context)
            {
                TransformedItemContexts = await consumeTask
            };

            await AggregateAsync(transformContext, context.Bundle.Transforms);

            transformContext.TransformedItemContexts = null;

            context.Result = await ApplyTransformsAsync(transformContext, context.Bundle.Transforms);
        }
    }
}
