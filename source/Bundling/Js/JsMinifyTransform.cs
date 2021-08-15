using System;

namespace Karambolo.AspNetCore.Bundling.Js
{
    public class JsMinifyTransform : BundleTransform
    {
        private readonly IJsMinifier _minifier;

        public JsMinifyTransform(IJsMinifier minifier)
        {
            if (minifier == null)
                throw new ArgumentNullException(nameof(minifier));

            _minifier = minifier;
        }

        public override void Transform(IBundleTransformContext context)
        {
            context.Content = _minifier.Process(context.Content, filePath: null);
        }
    }
}
