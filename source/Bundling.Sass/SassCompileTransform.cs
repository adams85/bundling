using System;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;

namespace Karambolo.AspNetCore.Bundling.Sass
{
    public class SassCompileTransform : BundleItemTransform
    {
        readonly ISassCompiler _compiler;

        public SassCompileTransform(ISassCompiler compiler)
        {
            if (compiler == null)
                throw new ArgumentNullException(nameof(compiler));

            _compiler = compiler;
        }

        public override async Task TransformAsync(IBundleItemTransformContext context)
        {
            string filePath;
            IFileProvider fileProvider;

            if (context is IFileBundleItemTransformContext fileItemContext)
            {
                filePath = fileItemContext.FilePath;
                fileProvider = fileItemContext.FileProvider;
            }
            else
            {
                filePath = null;
                fileProvider = null;
            }

            var pathPrefix = context.BuildContext.HttpContext.Request.PathBase + context.BuildContext.BundlingContext.StaticFilesPathPrefix;

            context.Content = await _compiler.CompileAsync(context.Content, pathPrefix, filePath, fileProvider, context.BuildContext.CancellationToken);
        }
    }
}