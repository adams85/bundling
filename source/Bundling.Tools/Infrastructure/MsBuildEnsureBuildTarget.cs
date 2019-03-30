using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Karambolo.AspNetCore.Bundling.Tools.Infrastructure
{
    public class MsBuildEnsureBuildTarget
    {
        private const string TargetName = "Build";

        private readonly IReporter _reporter;
        private readonly string _projectFile;
        private readonly OutputSink _outputSink;
        private readonly ProcessRunner _processRunner;

        public MsBuildEnsureBuildTarget(IReporter reporter, string projectFile)
            : this(reporter, projectFile, new OutputSink()) { }

        // output sink is for testing
        internal MsBuildEnsureBuildTarget(IReporter reporter, string projectFile, OutputSink outputSink)
        {
            if (reporter == null)
                throw new ArgumentNullException(nameof(reporter));

            if (projectFile == null)
                throw new ArgumentNullException(nameof(projectFile));

            if (outputSink == null)
                throw new ArgumentNullException(nameof(outputSink));

            _reporter = reporter;
            _projectFile = projectFile;
            _outputSink = outputSink;
            _processRunner = new ProcessRunner(reporter);
        }

        internal List<string> BuildFlags { get; } = new List<string>
        {
            "/nologo",
            "/v:n",
            "/t:" + TargetName,
            "/p:DotNetBundlingBuild=true", // extensibility point for users
            "/p:DesignTimeBuild=true", // don't do expensive things
        };

        public async Task<string> ExecuteAsync(string buildConfiguration, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var projectDir = Path.GetDirectoryName(_projectFile);

            OutputCapture capture = _outputSink.StartCapture();

            IEnumerable<string> args = new[]
            {
                "msbuild",
                _projectFile,
            }
            .Concat(BuildFlags);

            if (buildConfiguration != null)
                args = args.Append($"/p:Configuration=\"{buildConfiguration}\"");

            var processSpec = new ProcessSpec
            {
                Executable = DotNetMuxer.MuxerPathOrDefault(),
                WorkingDirectory = projectDir,
                Arguments = args,
                OutputCapture = capture
            };

            _reporter.Verbose($"Running MSBuild target '{TargetName}' on '{_projectFile}'");

            var exitCode = await _processRunner.RunAsync(processSpec, cancellationToken);
            if (exitCode != 0)
            {
                _reporter.Error($"Failed to build the project file '{Path.GetFileName(_projectFile)}'");
                DumpMSBuildOutput(capture);
                return null;
            }

            Match targetPathMatch = capture.Lines
                .Select(line => Regex.Match(line, @"Bundling\.TargetPath=<(.+)>"))
                .FirstOrDefault(match => match.Success);

            if (targetPathMatch == null)
            {
                _reporter.Error("Failed to determine the path of the output assembly.");
                DumpMSBuildOutput(capture);
                return null;
            }

            return targetPathMatch.Groups[1].Value;
        }

        private void DumpMSBuildOutput(OutputCapture capture)
        {
            _reporter.Output($"MSBuild output from target '{TargetName}':");
            _reporter.Output(string.Empty);

            foreach (var line in capture.Lines)
                _reporter.Output($"   {line}");

            _reporter.Output(string.Empty);
        }
    }
}
