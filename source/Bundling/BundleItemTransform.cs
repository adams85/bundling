using System.Threading.Tasks;

namespace Karambolo.AspNetCore.Bundling
{
    public interface IBundleItemTransform
    {
        void Transform(IBundleItemTransformContext context);
        Task TransformAsync(IBundleItemTransformContext context);
    }

    public class BundleItemTransform : IBundleItemTransform
    {
        public virtual void Transform(IBundleItemTransformContext context) { }

        public virtual Task TransformAsync(IBundleItemTransformContext context)
        {
            return Task.CompletedTask;
        }
    }
}
