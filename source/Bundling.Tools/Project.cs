using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Karambolo.AspNetCore.Bundling.Tools.Infrastructure;

namespace Karambolo.AspNetCore.Bundling.Tools
{
    internal class Project
    {
        private const string MetadataTag = "meta:";
        private const string GetMetadataTargetName = "GetBundlingProjectMetadata";

        private readonly string _file;
        private readonly string _framework;
        private readonly string _configuration;
        private readonly string _runtime;

        public Project(string file, string framework, string configuration, string runtime)
        {
            _file = file;
            _framework = framework;
            _configuration = configuration;
            _runtime = runtime;
            ProjectName = Path.GetFileName(file);
        }

        public string ProjectName { get; }

        public string AssemblyName { get; set; }
        public string OutputPath { get; set; }
        public string PlatformTarget { get; set; }
        public string ProjectAssetsFile { get; set; }
        public string ProjectDir { get; set; }
        public string RuntimeFrameworkVersion { get; set; }
        public string TargetFileName { get; set; }
        public string TargetFrameworkMoniker { get; set; }

        private static void DumpMSBuildOutput(OutputCapture capture)
        {
            Reporter.WriteInformation($"MSBuild output:");
            Reporter.WriteInformation(string.Empty);

            foreach (var line in capture.Lines)
                Reporter.WriteInformation($"  {line}");

            Reporter.WriteInformation(string.Empty);
        }

        public static bool IsMetadata(string value) => value.StartsWith(MetadataTag);

        private static Project FromMetadata(IReadOnlyDictionary<string, string> metadata, string file, string framework, string configuration, string runtime)
        {
            return new Project(file, framework, configuration, runtime)
            {
                AssemblyName = metadata["AssemblyName"],
                OutputPath = metadata["OutputPath"],
                ProjectAssetsFile = metadata["ProjectAssetsFile"],
                ProjectDir = metadata["ProjectDir"],
                RuntimeFrameworkVersion = metadata["RuntimeFrameworkVersion"],
                TargetFileName = metadata["TargetFileName"],
                TargetFrameworkMoniker = metadata["TargetFrameworkMoniker"]
            };
        }

        private static readonly Regex s_metadataRegex = new Regex(@"(?:^|;)(\w+)=(.*?)(?:$|;\w+=)");

        public static Project FromMetadata(string value, string framework = null, string configuration = null, string runtime = null)
        {
            value = value.Substring(MetadataTag.Length);

            var metadata = new Dictionary<string, string>();

            for (var index = 0; index < value.Length;)
            {
                var match = s_metadataRegex.Match(value, index);

                if (!match.Success)
                    break;

                var keyGroup = match.Groups[1];
                var valueGroup = match.Groups[2];

                metadata.Add(keyGroup.Value, valueGroup.Value);

                index = valueGroup.Index + valueGroup.Length;
            }

            return FromMetadata(metadata, null, framework, configuration, runtime);
        }

        public static async Task<Project> FromFileAsync(string file, string framework = null, string configuration = null, string runtime = null, CancellationToken cancellationToken = default)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            var args = new List<string>
            {
                "msbuild",
                file,
                "/nologo",
                "/v:n",
                "/t:" + GetMetadataTargetName,
            };

            if (framework != null)
                args.Add("/p:TargetFramework=" + framework);

            if (configuration != null)
                args.Add("/p:Configuration=" + configuration);

            if (runtime != null)
                args.Add("/p:RuntimeIdentifier=" + runtime);

            var capture = new OutputCapture();

            var processSpec = new ProcessSpec
            {
                Executable = "dotnet",
                Arguments = args,
                OutputCapture = capture
            };

            Reporter.WriteVerbose($"Running MSBuild target '{GetMetadataTargetName}' on '{file}'");

            var exitCode = await ProcessRunner.Default.RunAsync(processSpec, cancellationToken);

            if (exitCode != 0)
            {
                DumpMSBuildOutput(capture);
                throw new CommandException("Unable to retrieve project metadata. Please ensure it's an MSBuild-based .NET Core project which references the " + BundleBuilderProxy.BundlingAssemblyName + " NuGet package explicitly. " +
                    "If it's a multi-targeted project, you need to select one of the target frameworks by the '--framework' option.");
            }

            var metadata = capture.Lines
                .Select(line => Regex.Match(line, @"^\s*Bundling\.(\w+)=(.*)$"))
                .Where(match => match.Success)
                .ToDictionary(match => match.Groups[1].Value, match => match.Groups[2].Value);

            return FromMetadata(metadata, file, framework, configuration, runtime);
        }

        public async Task BuildAsync(CancellationToken cancellationToken)
        {
            var args = new List<string> { "build" };

            if (_file != null)
                args.Add(_file);

            // TODO: Only build for the first framework when unspecified
            if (_framework != null)
            {
                args.Add("--framework");
                args.Add(_framework);
            }

            if (_configuration != null)
            {
                args.Add("--configuration");
                args.Add(_configuration);
            }

            if (_runtime != null)
            {
                args.Add("--runtime");
                args.Add(_runtime);
            }

            args.Add("/v:q");
            args.Add("/nologo");

            var capture = new OutputCapture();

            var processSpec = new ProcessSpec
            {
                Executable = "dotnet",
                Arguments = args,
                OutputCapture = capture
            };

            Reporter.WriteInformation("Build started...");

            var exitCode = await ProcessRunner.Default.RunAsync(processSpec, cancellationToken);

            if (exitCode != 0)
            {
                DumpMSBuildOutput(capture);
                throw new CommandException("Build failed.");
            }

            Reporter.WriteInformation("Build succeeded.");
            Reporter.WriteInformation(string.Empty);
        }
    }
}
