using System;

namespace Karambolo.AspNetCore.Bundling.Js
{
    public class JsMinifyTransform : BundleTransform
    {
        readonly IJsMinifier _minifier;

        public JsMinifyTransform(IJsMinifier minifier)
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
