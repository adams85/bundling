using System;

namespace Karambolo.AspNetCore.Bundling.Css
{
    public class CssMinifyTransform : BundleTransform
    {
        readonly ICssMinifier _minifier;

        public CssMinifyTransform(ICssMinifier minifier)
        {
            if (minifier == null)
                throw new ArgumentNullException(nameof(minifier));

            _minifier = minifier;
        }

        public override void Transform(IBundleTransformContext context)
        {
            var filePath = context is IFileBundleItemTransformContext fileItemContext ? fileItemContext.FilePath : null;
            context.Content = _minifier.Process(context.Content, filePath);
        }
    }
}
