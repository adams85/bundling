using System.Threading.Tasks;

namespace Karambolo.AspNetCore.Bundling
{
    public interface IAllowsSourceIncludes { }

    public interface IBundleTransform
    {
        void Transform(IBundleTransformContext context);
        Task TransformAsync(IBundleTransformContext context);
    }

    public interface IAggregatorBundleTransform : IBundleTransform
    {
        void Aggregate(IBundleTransformContext context);
        Task AggregateAsync(IBundleTransformContext context);
    }

    public class BundleTransform : IBundleTransform
    {
        public virtual void Transform(IBundleTransformContext context) { }

        public virtual Task TransformAsync(IBundleTransformContext context)
        {
            return Task.CompletedTask;
        }
    }

    public class AggregatorBundleTransform : BundleTransform, IAggregatorBundleTransform
    {
        public virtual void Aggregate(IBundleTransformContext context) { }

        public virtual Task AggregateAsync(IBundleTransformContext context)
        {
            return Task.CompletedTask;
        }
    }
}
