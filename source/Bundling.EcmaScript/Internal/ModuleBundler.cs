using System;
using System.Collections.Generic;
using System.IO;
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
    internal partial class ModuleBundler : IModuleBundler
    {
        public const string DefaultExportName = "default";

        private static readonly char[] s_slashAndDot = new[] { '/', '.' };

        private static string NormalizePath(string path)
        {
            return UrlUtils.NormalizePath(path, canonicalize: true);
        }

        private static string NormalizeModulePath(string basePath, string path)
        {
            var index = path.LastIndexOfAny(s_slashAndDot);
            if (index < 0 || path[index] != '.')
                path += ".js";

            return NormalizePath(path.StartsWith("/", StringComparison.Ordinal) ? path : basePath + path);
        }

        private string GetFileProviderPrefix(ModuleFile moduleFile)
        {
            return FileProviderPrefixes[moduleFile.FileProvider];
        }

        private string GetFileProviderHint(ModuleFile moduleFile)
        {
            return
                moduleFile.FileProvider is PhysicalFileProvider physicalFileProvider ?
                $"{physicalFileProvider.GetType().Name}[{physicalFileProvider.Root}]" :
                $"{moduleFile.FileProvider.GetType().Name}{GetFileProviderPrefix(moduleFile)}";
        }

        private Uri GetFileUrl(ModuleFile moduleFile)
        {
            return
                !moduleFile.FilePath.StartsWith("/", StringComparison.Ordinal) ? new Uri("transient:" + moduleFile.FilePath) :
                moduleFile.FileProvider is PhysicalFileProvider physicalFileProvider ? new Uri(
                    Uri.UriSchemeFile + "://" + Path.Combine(physicalFileProvider.Root, moduleFile.FilePath.Substring(1)),
                    UriKind.Absolute) :
                new Uri("provider-file:" + GetFileProviderHint(moduleFile) + moduleFile.FilePath);
        }

        private readonly ILogger _logger;
        private readonly string _br;
        private readonly bool _developmentMode;

        public ModuleBundler(ILoggerFactory loggerFactory = null, ModuleBundlerOptions options = null)
        {
            _logger = loggerFactory?.CreateLogger<ModuleBundler>() ?? (ILogger)NullLogger.Instance;
            _br = options?.NewLine ?? Environment.NewLine;
            _developmentMode = options?.DevelopmentMode ?? false;

            Modules = new Dictionary<ModuleFile, ModuleData>();
            FileProviderPrefixes = new Dictionary<IFileProvider, string>();
        }

        internal Dictionary<ModuleFile, ModuleData> Modules { get; }
        internal Dictionary<IFileProvider, string> FileProviderPrefixes { get; }

        private async Task LoadModuleContent(ModuleData module)
        {
            try
            {
                using (Stream stream = module.File.GetFileInfo().CreateReadStream())
                using (var reader = new StreamReader(stream))
                    module.Content = await reader.ReadToEndAsync().ConfigureAwait(false);
            }
            catch (Exception ex) { throw _logger.ReadingModuleFileFailed(module.FilePath, GetFileProviderHint(module.File), ex); }
        }

        internal static ParserOptions CreateParserOptions()
        {
            return new ParserOptions() { AdaptRegexp = false, Comment = false, Tokens = false, Tolerant = false };
        }

        private Program ParseModuleContent(ModuleData module)
        {
            var parser = new JavaScriptParser(module.Content, module.ParserOptions);
            try { return parser.ParseModule(); }
            catch (Exception ex) { throw _logger.ParsingModuleFileFailed(module.FilePath, GetFileProviderHint(module.File), ex); }
        }

        private async Task LoadModuleCoreAsync(ModuleData module, CancellationTokenSource errorCts, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            try
            {
                module.ParserOptions = CreateParserOptions();
                module.Ast = ParseModuleContent(module);
                module.VariableScopes = new Dictionary<Node, VariableScope>();
                module.ModuleRefs = new Dictionary<ModuleFile, string>();
                module.ExportsRaw = new List<ExportData>();
                module.Imports = new Dictionary<string, ImportData>();

                AnalyzeDeclarations(module);

                if (module.ModuleRefs.Count == 0)
                    return;

                var loadModuleTasks = new List<Task>();
                foreach (ModuleFile moduleFile in module.ModuleRefs.Keys)
                {
                    lock (Modules)
                        if (!Modules.ContainsKey(moduleFile))
                            Modules.Add(moduleFile, module = new ModuleData(moduleFile));
                        else
                            continue;

                    loadModuleTasks.Add(LoadModuleAsync(module, errorCts, token));
                }
                await Task.WhenAll(loadModuleTasks).ConfigureAwait(false);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                errorCts.Cancel(); // stop loading process
                throw;
            }
        }

        private async Task LoadModuleAsync(ModuleData module, CancellationTokenSource errorCts, CancellationToken token)
        {
            await LoadModuleContent(module).ConfigureAwait(false);
            await LoadModuleCoreAsync(module, errorCts, token).ConfigureAwait(false);
        }

        internal async Task BundleCoreAsync(ModuleFile[] rootFiles, CancellationToken token)
        {
            int fileProviderId = 0;

            for (int i = 0, n = rootFiles.Length; i < n; i++)
            {
                ModuleFile moduleFile = rootFiles[i];

                if (Modules.ContainsKey(moduleFile))
                    continue;

                rootFiles[i] = moduleFile = new ModuleFile(
                    moduleFile.FileProvider,
                    moduleFile.FilePath != null ? NormalizePath(moduleFile.FilePath) : $"<root{i}>",
                    moduleFile.CaseSensitiveFilePaths)
                {
                    Content = moduleFile.Content
                };

                if (!FileProviderPrefixes.ContainsKey(moduleFile.FileProvider))
                    FileProviderPrefixes.Add(moduleFile.FileProvider, "$" + fileProviderId++);

                var module = new ModuleData(moduleFile);

                if (module.Content == null)
                    await LoadModuleContent(module).ConfigureAwait(false);

                Modules.Add(moduleFile, module);
            }

            if (FileProviderPrefixes.Count == 1)
                FileProviderPrefixes[FileProviderPrefixes.Keys.First()] = string.Empty;

            // analyze

            using (var errorCts = new CancellationTokenSource())
            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(errorCts.Token, token))
                await Task.WhenAll(Modules.Values.ToArray().Select(module => LoadModuleCoreAsync(module, errorCts, linkedCts.Token))).ConfigureAwait(false);

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
        }

        public async Task<ModuleBundlingResult> BundleAsync(ModuleFile[] rootFiles, CancellationToken token = default)
        {
            if (rootFiles == null)
                throw new ArgumentNullException(nameof(rootFiles));

            rootFiles = rootFiles.ToArray();

            try
            {
                await BundleCoreAsync(rootFiles, token).ConfigureAwait(false);

                // aggregate

                token.ThrowIfCancellationRequested();

                return BuildResult(rootFiles);
            }
            finally
            {
                Modules.Clear();
                FileProviderPrefixes.Clear();
            }
        }
    }
}
