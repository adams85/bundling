using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;

namespace Karambolo.AspNetCore.Bundling.Less
{
    public class LessCompileTransform : BundleItemTransform
    {
        private readonly ILessCompiler _compiler;

        public LessCompileTransform(ILessCompiler compiler)
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

            LessCompilationResult result = await _compiler.CompileAsync(context.Content, pathPrefix, filePath, fileProvider, context.BuildContext.CancellationToken);

            context.Content = result.Content ?? string.Empty;
            if (fileItemContext != null && result.Imports != null)
                (fileItemContext.AdditionalSourceFilePaths ?? (fileItemContext.AdditionalSourceFilePaths = new HashSet<string>())).UnionWith(result.Imports);
        }
    }
}
