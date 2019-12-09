using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Karambolo.AspNetCore.Bundling.Tools.Infrastructure;
using Microsoft.DotNet.Cli.CommandLine;

namespace Karambolo.AspNetCore.Bundling.Tools
{
    internal class RootCommandNested : RootCommandBase
    {
        private CommandOption _assemblyOption;
        private CommandOption _projectDirOption;

        protected override void ConfigureCore(CommandLineApplication command)
        {
            _assemblyOption = command.Option("--assembly <PATH>", string.Empty);
            _projectDirOption = command.Option("--project-dir <PATH>", string.Empty);
        }

        protected string AssemblyPath => _assemblyOption.Value();
        protected string ProjectDirPath => _projectDirOption.Value();

        protected override async Task<int> ExecuteAsync(CancellationToken cancellationToken)
        {
            string configFilePath;
            if (ConfigSources.HasFlag(ConfigSources.ConfigFile))
                configFilePath = ConfigFilePath ?? GetDefaultConfigFilePath(ProjectDirPath);
            else
                configFilePath = null;

            string compilationBasePath = Path.GetDirectoryName(AssemblyPath);

            IEnumerable<string> assemblyFilePaths;
            if (ConfigSources.HasFlag(ConfigSources.AppAssembly) || ConfigSources.HasFlag(ConfigSources.OutputAssemblies))
            {
                if (ConfigSources.HasFlag(ConfigSources.OutputAssemblies))
                    assemblyFilePaths = Directory.EnumerateFiles(compilationBasePath, "*.dll", SearchOption.TopDirectoryOnly)
                        .Where(path => !Path.GetFileName(path).StartsWith(BundleBuilderProxy.BundlingAssemblyName));
                else
                    assemblyFilePaths = Enumerable.Empty<string>();

                if (ConfigSources.HasFlag(ConfigSources.AppAssembly))
                    assemblyFilePaths = assemblyFilePaths.Prepend(AssemblyPath);

                assemblyFilePaths = assemblyFilePaths.Distinct();
            }
            else
                assemblyFilePaths = Enumerable.Empty<string>();

            var bundleBuilderProxy = new BundleBuilderProxy(AssemblyLoadContext.Default, ReporterAdapter.Default);

            var settings = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["ProjectDirPath"] = ProjectDirPath,
                ["CompilationBasePath"] = compilationBasePath,
                ["Mode"] = BundlingMode.ToString(),
                ["Logger"] = new Action<int, string>(Log)
            };

            if (configFilePath != null)
                settings["ConfigFilePath"] = Path.GetFullPath(configFilePath);

            await bundleBuilderProxy.ProcessConfigurationsAsync(assemblyFilePaths, settings, cancellationToken);

            return 0;
        }

        private string GetDefaultConfigFilePath(string projectDirPath)
        {
            var path = Path.Combine(projectDirPath, "bundleconfig.json");
            return File.Exists(path) ? path : null;
        }

        private static void Log(int level, string message)
        {
            switch (level)
            {
                case -1:
                    Reporter.WriteVerbose(message);
                    return;
                case 0:
                    Reporter.WriteInformation(message);
                    return;
                case 1:
                    Reporter.WriteWarning(message);
                    return;
                case 2:
                    Reporter.WriteError(message);
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
