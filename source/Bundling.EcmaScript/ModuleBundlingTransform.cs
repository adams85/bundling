using System;
using System.Linq;
using System.Threading.Tasks;

namespace Karambolo.AspNetCore.Bundling.EcmaScript
{
    public class ModuleBundlingTransform : AggregatorBundleTransform
    {
        private readonly IModuleBundlerFactory _moduleBundlerFactory;
        private readonly ModuleBundlerOptions _options;

        public ModuleBundlingTransform(IModuleBundlerFactory moduleBundlerFactory, ModuleBundlerOptions options)
        {
            if (moduleBundlerFactory == null)
                throw new ArgumentNullException(nameof(moduleBundlerFactory));

            _moduleBundlerFactory = moduleBundlerFactory;
            _options = options;
        }

        public override async Task AggregateAsync(IBundleTransformContext context)
        {
            IModuleBundler bundler = _moduleBundlerFactory.Create(_options);

            ModuleFile[] rootFiles = context.TransformedItemContexts
                .Select((itemContext, i) =>
                    itemContext is IFileBundleItemTransformContext fileItemContext ?
                    new ModuleFile(fileItemContext.FileProvider, fileItemContext.FilePath, fileItemContext.CaseSensitiveFilePaths) { Content = fileItemContext.Content } :
                    new ModuleFile() { Content = itemContext.Content })
                .ToArray();

            ModuleBundlingResult result = await bundler.BundleAsync(rootFiles, context.BuildContext.CancellationToken);

            context.Content = result.Content ?? string.Empty;
            if (result.Imports != null && result.Imports.Count > 0)
                context.BuildContext.ChangeSources?.UnionWith(result.Imports);
        }
    }
}
