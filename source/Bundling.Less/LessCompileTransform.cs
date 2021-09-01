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
            bool caseSensitiveFilePaths;

            if (context is IFileBundleItemTransformContext fileItemContext)
            {
                filePath = fileItemContext.FilePath;
                fileProvider = fileItemContext.FileProvider;
                caseSensitiveFilePaths = fileItemContext.CaseSensitiveFilePaths;
            }
            else
            {
                filePath = null;
                fileProvider = null;
                caseSensitiveFilePaths = true;
            }

            PathString pathPrefix = context.BuildContext.BundlingContext.StaticFilesPathPrefix;
            PathString outputPath = context.BuildContext.BundlingContext.BundlesPathPrefix + context.BuildContext.Bundle.Path;

            LessCompilationResult result = await _compiler.CompileAsync(context.Content, pathPrefix, filePath, fileProvider, outputPath, context.BuildContext.CancellationToken);

            context.Content = result.Content ?? string.Empty;
            if (result.Imports != null && result.Imports.Count > 0)
                context.BuildContext.ChangeSources?.UnionWith(result.Imports.Select(import => new AbstractionFile(fileProvider, import, caseSensitiveFilePaths)));
        }
    }
}
