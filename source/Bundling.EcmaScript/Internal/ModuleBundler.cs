using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Esprima;
using Esprima.Ast;
using Karambolo.AspNetCore.Bundling.EcmaScript.Internal.Helpers;
using Karambolo.AspNetCore.Bundling.Internal.Helpers;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Karambolo.AspNetCore.Bundling.EcmaScript.Internal
{
    internal sealed partial class ModuleBundler : IModuleBundler
    {
        public const string DefaultExportName = "default";

        private readonly ILogger _logger;
        private readonly string _br;
        private readonly bool _developmentMode;

        public ModuleBundler(ILoggerFactory loggerFactory = null, ModuleBundlerOptions options = null)
        {
            _logger = loggerFactory?.CreateLogger<ModuleBundler>() ?? (ILogger)NullLogger.Instance;
            _br = options?.NewLine ?? Environment.NewLine;
            _developmentMode = options?.DevelopmentMode ?? false;

            Files = new Dictionary<FileModuleResource, (bool, Task<string>)>(new FileModuleResource.AbstractionFileComparer());
            Modules = new Dictionary<IModuleResource, ModuleData>();
        }

        internal Dictionary<FileModuleResource, (bool IsRoot, Task<string> Content)> Files { get; }
        internal Dictionary<IModuleResource, ModuleData> Modules { get; }

        private async Task LoadModuleContentAsync(ModuleData module, CancellationToken token)
        {
            try
            {
                if (module.Resource is FileModuleResource fileResource)
                {
                    (bool IsRoot, Task<string> Content) fileData;

                    lock (Files)
                        if (!Files.TryGetValue(fileResource, out fileData))
                            Files.Add(fileResource, fileData = (IsRoot: false, Content: fileResource.LoadContentAsync(token)));

                    module.Content = await fileData.Content.ConfigureAwait(false);
                }
                else
                    module.Content = await module.Resource.LoadContentAsync(token).ConfigureAwait(false);
            }
            catch (Exception ex) { throw _logger.LoadingModuleFailed(module.Resource.Url.ToString(), ex); }
        }

        internal static ParserOptions CreateParserOptions()
        {
            return new ParserOptions { AdaptRegexp = false, Comments = false, Tokens = false, Tolerant = false };
        }

        private Program ParseModuleContent(ModuleData module)
        {
            var parser = new JavaScriptParser(module.ParserOptions);
            try { return parser.ParseModule(module.Content); }
            catch (Exception ex) { throw _logger.ParsingModuleFailed(module.Resource.Url.ToString(), ex); }
        }

        private async Task LoadModuleAsync(ModuleData module, CancellationTokenSource errorCts, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            try
            {
                await LoadModuleContentAsync(module, token).ConfigureAwait(false);

                module.ParserOptions = CreateParserOptions();
                module.Ast = ParseModuleContent(module);
                module.ModuleRefs = new Dictionary<IModuleResource, string>();
                module.ExportsRaw = new List<ExportData>();
                module.Imports = new Dictionary<string, ImportData>();

                AnalyzeDeclarations(module);

                if (module.ModuleRefs.Count > 0)
                {
                    var modulesToLoad = new List<ModuleData>();

                    var normalizedModuleRefs = new Dictionary<IModuleResource, string>(module.ModuleRefs.Count);

                    lock (Modules)
                        foreach ((IModuleResource resource, string moduleRef) in module.ModuleRefs)
                        {
                            if (!Modules.TryGetValue(resource, out ModuleData knownModule))
                            {
                                Modules.Add(resource, knownModule = new ModuleData(resource));
                                modulesToLoad.Add(knownModule);
                            }

                            normalizedModuleRefs.Add(knownModule.Resource, moduleRef);
                        }

                    module.ModuleRefs = normalizedModuleRefs;

                    await LoadModulesAsync(modulesToLoad, errorCts, token).ConfigureAwait(false);
                }
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                errorCts.Cancel(); // stop loading process
                throw;
            }
        }

        private Task LoadModulesAsync(IEnumerable<ModuleData> modules, CancellationTokenSource errorCts, CancellationToken token)
        {
            return Task.WhenAll(modules.Select(module => LoadModuleAsync(module, errorCts, token)));
        }

        private ModuleData[] GetRootModules(ModuleFile[] rootFiles, CancellationToken token)
        {
            var fileProviderPrefixes = new Dictionary<IFileProvider, string>();
            int fileProviderId = 0;

            for (int i = 0, n = rootFiles.Length; i < n; i++)
            {
                ModuleFile moduleFile = rootFiles[i];

                if (moduleFile == null)
                    throw ErrorHelper.ArrayCannotContainNull(nameof(rootFiles));

                if (moduleFile.FileProvider != null && !fileProviderPrefixes.ContainsKey(moduleFile.FileProvider))
                    fileProviderPrefixes.Add(moduleFile.FileProvider, "$" + (fileProviderId++).ToString(CultureInfo.InvariantCulture));
            }

            if (fileProviderPrefixes.Count == 1)
                fileProviderPrefixes[fileProviderPrefixes.Keys.First()] = string.Empty;

            var duplicateRootFiles = false;
            var rootModules = new ModuleData[rootFiles.Length];

            for (int i = 0, n = rootFiles.Length; i < n; i++)
            {
                ModuleFile moduleFile = rootFiles[i];

                ref ModuleData module = ref rootModules[i];
                IModuleResource resource;

                if (moduleFile.FileProvider != null && moduleFile.FilePath != null)
                {
                    var fileResource = new FileModuleResource(fileProviderPrefixes[moduleFile.FileProvider], moduleFile)
                    {
                        Content = moduleFile.Content
                    };

                    if (Files.ContainsKey(fileResource))
                    {
                        duplicateRootFiles = true;
                        continue;
                    }

                    Files.Add(fileResource, (IsRoot: true, Content: fileResource.LoadContentAsync(token)));

                    resource = fileResource;
                }
                else
                    resource = new TransientModuleResource(i, moduleFile.Content, moduleFile.FileProvider != null ? fileProviderPrefixes[moduleFile.FileProvider] : null, moduleFile);

                module = new ModuleData(resource);

                Modules.Add(resource, module);
            }

            if (duplicateRootFiles)
                rootModules = rootModules.Where(module => module != null).ToArray();

            return rootModules;
        }

        internal async Task<ModuleData[]> BundleCoreAsync(ModuleFile[] rootFiles, CancellationToken token)
        {
            ModuleData[] rootModules = GetRootModules(rootFiles, token);

            // analyze

            using (var errorCts = new CancellationTokenSource())
            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(errorCts.Token, token))
                await LoadModulesAsync(rootModules, errorCts, linkedCts.Token).ConfigureAwait(false);

            // rewrite

            try
            {
                Parallel.ForEach(Modules.Values, new ParallelOptions { CancellationToken = token },
                    () => new RewriteModuleLocals(),
                    RewriteModule,
                    _ => { });
            }
            catch (AggregateException ex)
            {
                // unwrap and re-throw aggregate exception
                ExceptionDispatchInfo.Capture(ex.Flatten().InnerException).Throw();
            }

            return rootModules;
        }

        public async Task<ModuleBundlingResult> BundleAsync(ModuleFile[] rootFiles, CancellationToken token = default)
        {
            if (rootFiles == null)
                throw new ArgumentNullException(nameof(rootFiles));

            try
            {
                ModuleData[] rootModules = await BundleCoreAsync(rootFiles, token).ConfigureAwait(false);

                // aggregate

                token.ThrowIfCancellationRequested();

                return BuildResult(rootModules);
            }
            finally
            {
                Files.Clear();
                Modules.Clear();
            }
        }
    }
}
