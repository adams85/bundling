using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Karambolo.AspNetCore.Bundling.Tools.Infrastructure;
using Microsoft.Extensions.CommandLineUtils;

namespace Karambolo.AspNetCore.Bundling.Tools
{
    public partial class Program : IDisposable
    {
        public static async Task Main(string[] args)
        {
            using (var program = new Program(PhysicalConsole.Singleton, Directory.GetCurrentDirectory()))
                await program.RunAsync(args);
        }

        private static IReporter CreateReporter(bool verbose, bool quiet, IConsole console)
        {
            return new ConsoleReporter(console, verbose || CliContext.IsGlobalVerbose(), quiet);
        }

        private readonly IConsole _console;
        private readonly string _workingDir;
        private readonly CancellationTokenSource _cts;
        private IReporter _reporter;

        public Program(IConsole console, string workingDir)
        {
            _console = console;
            _workingDir = workingDir;
            _cts = new CancellationTokenSource();
            _console.CancelKeyPress += CancelKeyPress;
            _reporter = CreateReporter(verbose: true, quiet: false, console: _console);
        }

        public void Dispose()
        {
            _console.CancelKeyPress -= CancelKeyPress;
            _cts.Dispose();
        }

        private void CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            _cts.Cancel();
        }

        public async Task<int> RunAsync(string[] args)
        {
            CommandLineOptions options;
            try
            {
                options = CommandLineOptions.Parse(args, _console);
            }
            catch (CommandParsingException ex)
            {
                _reporter.Error(ex.Message);
                return 1;
            }

            if (options == null)
                // invalid args syntax
                return 1;

            if (options.IsHelp)
                return 2;

            // update reporter as configured by options
            _reporter = CreateReporter(options.IsVerbose, options.IsQuiet, _console);

            try
            {
                _cts.Token.ThrowIfCancellationRequested();

                await RunCoreAsync(options);

                return await Task.FromResult(0);
            }
            catch (OperationCanceledException)
            {
                // swallow when only exception is the CTRL+C forced an exit
                return 0;
            }
            catch (Exception ex)
            {
                _reporter.Error(ex.ToString());

                if (ex is ReflectionTypeLoadException reflectionTypeLoadException)
                    foreach (Exception loaderException in reflectionTypeLoadException.LoaderExceptions)
                        _reporter.Error(loaderException.ToString());

                _reporter.Error("An unexpected error occurred");
                return 1;
            }
        }

        private string GetDefaultConfigFilePath(string projectDirPath)
        {
            var path = Path.Combine(projectDirPath, "bundleconfig.json");
            return File.Exists(path) ? path : null;
        }

        private async Task<int> RunCoreAsync(CommandLineOptions options)
        {
            string projectFilePath;
            try { projectFilePath = MsBuildProjectFinder.FindMsBuildProject(_workingDir, options.Project); }
            catch (FileNotFoundException ex)
            {
                _reporter.Error(ex.Message);
                return 1;
            }

            var projectDirPath = Path.GetDirectoryName(projectFilePath);

            string configFilePath;
            if (options.ConfigSources.HasFlag(ConfigSources.ConfigFile))
                configFilePath = options.ConfigFile ?? GetDefaultConfigFilePath(projectDirPath);
            else
                configFilePath = null;

            string compilationBasePath;
            IEnumerable<string> assemblyFilePaths;
            if (options.ConfigSources.HasFlag(ConfigSources.AppAssembly) || options.ConfigSources.HasFlag(ConfigSources.OutputAssemblies))
            {
                var targetFilePath = options.BuildTargetPath;
                if (targetFilePath == null)
                {
                    _reporter.Output($"Building project {projectFilePath}...");

                    targetFilePath = await new MsBuildEnsureBuildTarget(_reporter, projectFilePath).ExecuteAsync(options.BuildConfiguration, _cts.Token);

                    // we need the project to be built if configuration is specified by code
                    if (targetFilePath == null)
                        return 1;
                }

                compilationBasePath = Path.GetDirectoryName(targetFilePath);

                if (options.ConfigSources.HasFlag(ConfigSources.OutputAssemblies))
                    assemblyFilePaths = Directory.EnumerateFiles(compilationBasePath, "*.dll", SearchOption.TopDirectoryOnly);
                else
                    assemblyFilePaths = Enumerable.Empty<string>();

                if (options.ConfigSources.HasFlag(ConfigSources.AppAssembly))
                    assemblyFilePaths = assemblyFilePaths.Prepend(targetFilePath);

                assemblyFilePaths = assemblyFilePaths.Distinct();
            }
            else
            {
                if (options.BuildTargetPath == null)
                {
                    // TODO: prefer release build?
                    compilationBasePath = Directory.EnumerateFiles(projectDirPath, BundleBuilderProxy.BundlingAssemblyName + ".dll", SearchOption.AllDirectories).FirstOrDefault();
                    if (compilationBasePath != null)
                        compilationBasePath = Path.GetDirectoryName(compilationBasePath);
                }
                else
                    compilationBasePath = Path.GetDirectoryName(options.BuildTargetPath);

                assemblyFilePaths = Enumerable.Empty<string>();
            }

            // we load the application into a separate AssemblyLoadContext (to avoid assembly version mismatches),
            // then we pass the discovered bundling configurations to the design-time bundle builder implementation residing in the main assembly

            var assemblyLoader = new AssemblyLoader(compilationBasePath);

            var bundleBuilderProxy = new BundleBuilderProxy(assemblyLoader, _reporter);

            var settings = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["ProjectFilePath"] = projectFilePath,
                ["CompilationBasePath"] = compilationBasePath,
                ["Mode"] = options.Mode.ToString(),
                ["Logger"] = new Action<int, string>(Log)
            };

            if (configFilePath != null)
                settings["ConfigFilePath"] = configFilePath;

            await bundleBuilderProxy.ProcessConfigurationsAsync(assemblyFilePaths, settings, _cts.Token);

            return 0;
        }

        private void Log(int level, string message)
        {
            switch (level)
            {
                case -1:
                    _reporter.Verbose(message);
                    return;
                case 0:
                    _reporter.Output(message);
                    return;
                case 1:
                    _reporter.Warn(message);
                    return;
                case 2:
                    _reporter.Error(message);
                    return;
                default:
                    if (level < -1)
                        goto case -1;
                    else
                        goto case 2;
            }
        }
    }
}
