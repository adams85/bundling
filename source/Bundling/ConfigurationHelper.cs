using System.Collections.Generic;

namespace Karambolo.AspNetCore.Bundling
{
    public interface IConfigurationHelper
    {
        string Type { get; }
        string OutputMediaType { get; }
        bool CanRenderSourceIncludes { get; }

        IReadOnlyList<IBundleItemTransform> SetDefaultItemTransforms(IReadOnlyList<IBundleItemTransform> itemTransforms);
        IReadOnlyList<IBundleTransform> SetDefaultTransforms(IReadOnlyList<IBundleTransform> transforms);
        IReadOnlyList<IBundleTransform> EnableMinification(IReadOnlyList<IBundleTransform> transforms);
    }
}
