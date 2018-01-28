using System.Threading.Tasks;

namespace Karambolo.AspNetCore.Bundling
{
    public interface IBundleTransform
    {
        void Transform(IBundleTransformContext context);
        Task TransformAsync(IBundleTransformContext context);
    }

    public class BundleTransform : IBundleTransform
    {
        public virtual void Transform(IBundleTransformContext context) { }

        public virtual Task TransformAsync(IBundleTransformContext context)
        {
            return Task.CompletedTask;
        }
    }
}
