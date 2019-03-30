using System;
using System.Linq;
using System.Threading.Tasks;
using Karambolo.AspNetCore.Bundling.Internal;
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

            PathString pathPrefix = context.BuildContext.AppBasePath + context.BuildContext.BundlingContext.StaticFilesPathPrefix;

            LessCompilationResult result = await _compiler.CompileAsync(context.Content, pathPrefix, filePath, fileProvider, context.BuildContext.CancellationToken);

            context.Content = result.Content ?? string.Empty;
            if (result.Imports != null && result.Imports.Count > 0)
                context.BuildContext.ChangeSources?.UnionWith(result.Imports.Select(import => new AbstractionFile(fileProvider, import)));
        }
    }
}
