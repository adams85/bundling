using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Karambolo.AspNetCore.Bundling.Internal
{
    public class DefaultBundleBuilder : IBundleBuilder
    {
        protected virtual void CreateItemTransformPipeline(IBundleBuilderContext context, out ITargetBlock<IBundleSourceBuildItem> input, out ISourceBlock<string> output)
        {
            var transformBlock = new TransformBlock<IBundleSourceBuildItem, string>(it => ApplyItemTransformsAsync(it.ItemTransformContext, it.ItemTransforms),
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

        protected virtual async Task<string> ApplyItemTransformsAsync(IBundleItemTransformContext context, IReadOnlyList<IBundleItemTransform> transforms)
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

            return context.Content;
        }

        protected virtual async Task<string> ApplyTransformsAsync(IBundleTransformContext context, IReadOnlyList<IBundleTransform> transforms)
        {
            if (transforms != null)
            {
                var n = transforms.Count;
                for (var i = 0; i < n; i++)
                {
                    context.BuildContext.CancellationToken.ThrowIfCancellationRequested();

                    IBundleTransform transform = transforms[i];
                    await transform.TransformAsync(context);
                    transform.Transform(context);
                }
            }

            return context.Content;
        }

        public virtual async Task BuildAsync(IBundleBuilderContext context)
        {
            CreateItemTransformPipeline(context, out ITargetBlock<IBundleSourceBuildItem> input, out ISourceBlock<string> output);

            // consumer
            async Task<string> ConsumeAsync()
            {
                var appendToken = false;
                var sb = new StringBuilder();
                while (await output.OutputAvailableAsync(context.CancellationToken))
                {
                    if (appendToken)
                        sb.Append(context.Bundle.ConcatenationToken);
                    else
                        appendToken = true;

                    sb.Append(output.Receive());
                }
                return sb.ToString();
            };

            Task<string> consumeTask = ConsumeAsync();

            // producer
            var n = context.Bundle.Sources.Length;
            for (var i = 0; i < n; i++)
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                IBundleSourceModel source = context.Bundle.Sources[i];
                await source.ProvideBuildItemsAsync(context, it => input.Post(it));
            }
            input.Complete();

            // getting result
            var transformContext = new BundleTransformContext(context)
            {
                Content = await consumeTask
            };

            context.Result = await ApplyTransformsAsync(transformContext, context.Bundle.Transforms);
        }
    }
}
