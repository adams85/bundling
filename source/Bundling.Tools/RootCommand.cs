using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Karambolo.AspNetCore.Bundling.Tools.Infrastructure;
using Microsoft.DotNet.Cli.CommandLine;

namespace Karambolo.AspNetCore.Bundling.Tools
{
    internal class RootCommand : RootCommandBase
    {
        private readonly string[] _originalArgs;

        private CommandOption _projectOption;
        private CommandOption _frameworkOption;
        private CommandOption _configurationOption;
        private CommandOption _runtimeOption;
        private CommandOption _noBuildOption;

        public RootCommand(string[] originalArgs)
        {
            _originalArgs = originalArgs;
        }

        protected string ProjectPath => _projectOption.Value();
        protected string Framework => _frameworkOption.Value();
        protected string Configuration => _configurationOption.Value();
        protected string Runtime => _runtimeOption.Value();
        protected bool NoBuild => _noBuildOption.HasValue();

        protected override void ConfigureCore(CommandLineApplication command)
        {
            _projectOption = command.Option("-p|--project <PROJECT>", "The location of the project directory or project file whose web assets needs to be bundled.");
            _frameworkOption = command.Option("--framework <FRAMEWORK>", "The target framework (when build is necessary).");
            _configurationOption = command.Option("--configuration <CONFIGURATION>", "The configuration to use (when build is necessary).");
            _runtimeOption = command.Option("--runtime <RUNTIME_IDENTIFIER>", "The runtime to use (when build is necessary).");
            _noBuildOption = command.Option("--no-build", "Don't build the project. Only use this when the build is up-to-date.");

            command.VersionOption("--version", GetVersion);
            command.HelpOption("-?|-h|--help");
        }

        protected override async Task<int> ExecuteAsync(CancellationToken cancellationToken)
        {
            var projectPath = ProjectPath ?? Directory.GetCurrentDirectory();
            Project project;

            // project metadata needs to be determined
            if (!Project.IsMetadata(projectPath))
            {
                var projectFile = ResolveProjectFile(projectPath);

                Reporter.WriteVerbose($"Using project '{projectFile}'.");

                project = await Project.FromFileAsync(projectFile, Framework, Configuration, Runtime, cancellationToken);

                if (!NoBuild && (ConfigSources.HasFlag(ConfigSources.AppAssembly) || ConfigSources.HasFlag(ConfigSources.OutputAssemblies)))
                    await project.BuildAsync(cancellationToken);
            }
            // project metadata is available as application is called during build process
            else
                project = Project.FromMetadata(projectPath, Framework, Configuration, Runtime);

            var toolsPath = typeof(Program).GetTypeInfo().Assembly.Location;

            var targetDir = Path.GetFullPath(Path.Combine(project.ProjectDir, project.OutputPath));
            var targetPath = Path.Combine(targetDir, project.TargetFileName);

            var depsFile = Path.Combine(targetDir, project.AssemblyName + ".deps.json");
            var runtimeConfig = Path.Combine(targetDir, project.AssemblyName + ".runtimeconfig.json");
            var projectAssetsFile = project.ProjectAssetsFile;

            var args = new List<string>() { "exec" };

            args.Add("--depsfile");
            args.Add(depsFile);

            if (!string.IsNullOrEmpty(projectAssetsFile))
            {
                using (var reader = JsonDocument.Parse(File.OpenRead(projectAssetsFile)))
                {
                    var projectAssets = reader.RootElement;
                    var packageFolders = projectAssets.GetProperty("packageFolders").EnumerateObject().Select(p => p.Name);

                    foreach (var packageFolder in packageFolders)
                    {
                        args.Add("--additionalprobingpath");
                        args.Add(packageFolder.TrimEnd(Path.DirectorySeparatorChar));
                    }
                }
            }

            if (File.Exists(runtimeConfig))
            {
                args.Add("--runtimeconfig");
                args.Add(runtimeConfig);
            }
            else if (project.RuntimeFrameworkVersion.Length != 0)
            {
                args.Add("--fx-version");
                args.Add(project.RuntimeFrameworkVersion);
            }

            args.Add(toolsPath);

            args.AddRange(_originalArgs);

            args.Add("--assembly");
            args.Add(targetPath);
            args.Add("--project-dir");
            args.Add(project.ProjectDir);

            var processSpec = new ProcessSpec
            {
                Executable = "dotnet",
                Arguments = args,
                EnvironmentVariables = { [Program.NestedRunEnvironmentVariableName] = bool.TrueString },
                WorkingDirectory = Directory.GetCurrentDirectory()
            };

            return await ProcessRunner.Default.RunAsync(processSpec, cancellationToken);
        }

        private static string ResolveProjectFile(string projectPath)
        {
            var projects = ResolveProjectFileCore(projectPath);

            if (projects.Length > 1)
            {
                throw new CommandException(
                    projectPath != null ?
                    $"More than one project was found in directory '{projectPath}'. Specify one using its file name." :
                    "More than one project was found in the current working directory. Use the --project option.");
            }
            else if (projects.Length < 1)
            {
                throw new CommandException(
                    projectPath != null ?
                    $"No project was found in directory '{projectPath}'." :
                    "No project was found. Change the current working directory or use the --project option.");
            }

            return projects[0];
        }

        private static string[] ResolveProjectFileCore(string path)
        {
            if (path == null)
            {
                path = Directory.GetCurrentDirectory();
            }
            else if (!Directory.Exists(path)) // It's not a directory
            {
                return new[] { path };
            }

            var projectFiles = Directory.EnumerateFiles(path, "*.*proj", SearchOption.TopDirectoryOnly)
                .Where(f => !string.Equals(Path.GetExtension(f), ".xproj", StringComparison.OrdinalIgnoreCase))
                .Take(2)
                .ToArray();

            return projectFiles;
        }

        private static string GetVersion()
        {
            var assembly = typeof(RootCommand).GetTypeInfo().Assembly;

            var attribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();

            return attribute != null ? attribute.InformationalVersion : assembly.GetName().Version.ToString();
        }
    }
}
