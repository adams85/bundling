using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;

namespace Karambolo.AspNetCore.Bundling.Sass
{
    public class SassCompileTransform : BundleItemTransform
    {
        private readonly ISassCompiler _compiler;

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
            var fileItemContext = context as IFileBundleItemTransformContext;

            if (fileItemContext != null)
            {
                filePath = fileItemContext.FilePath;
                fileProvider = fileItemContext.FileProvider;
            }
            else
            {
                filePath = null;
                fileProvider = null;
            }

            PathString pathPrefix = context.BuildContext.HttpContext.Request.PathBase + context.BuildContext.BundlingContext.StaticFilesPathPrefix;

            SassCompilationResult result = await _compiler.CompileAsync(context.Content, pathPrefix, filePath, fileProvider, context.BuildContext.CancellationToken);

            context.Content = result.Content ?? string.Empty;
            if (result.Imports != null)
                (fileItemContext.AdditionalSourceFilePaths ?? (fileItemContext.AdditionalSourceFilePaths = new HashSet<string>())).UnionWith(result.Imports);
        }
    }
}
