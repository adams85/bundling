using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.CommandLineUtils;
using Karambolo.AspNetCore.Bundling.Tools.Infrastructure;

namespace Karambolo.AspNetCore.Bundling.Tools
{
    [Flags]
    internal enum ConfigSources
    {
        None = 0,
        ConfigFile = 0x1,
        AppAssembly = 0x2,
        OutputAssemblies = 0x4,
        Default = ConfigFile | AppAssembly
    }

    internal enum BundlingMode
    {
        Production,
        Development,
    }

    // https://github.com/aspnet/Extensions/blob/2.0.0/shared/Microsoft.Extensions.CommandLineUtils.Sources/CommandLine/CommandLineApplication.cs
    internal class CommandLineOptions
    {
        private static readonly string s_name = "dotnet bundle";

        public string Project { get; private set; }
        public string BuildConfiguration { get; private set; }
        public string BuildTargetPath { get; private set; }
        public ConfigSources ConfigSources { get; private set; }
        public string ConfigFile { get; private set; }
        public BundlingMode Mode { get; private set; }
        public bool IsHelp { get; private set; }
        public bool IsQuiet { get; private set; }
        public bool IsVerbose { get; private set; }
        public IList<string> RemainingArguments { get; private set; }

        public static CommandLineOptions Parse(string[] args, IConsole console)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));

            if (console == null)
                throw new ArgumentNullException(nameof(console));

            var app = new CommandLineApplication(throwOnUnexpectedArg: false)
            {
                Name = s_name,
                FullName = $".NET Core CLI tools for the Karambolo.AspNetCore.Bundling library",
                Out = console.Out,
                Error = console.Error,
                AllowArgumentSeparator = true,
                ExtendedHelpText = $@"
Remarks:
  The special option '--' is used to delimit the end of the options and
  the beginning of arguments that will be passed to the child dotnet process.
  Its use is optional. When the special option '--' is not used,
  {s_name} will use the first unrecognized argument as the beginning
  of all arguments passed into the child dotnet process.

  For example: {s_name} -- --verbose

  Even though '--verbose' is an option {s_name} supports, the use of '--'
  indicates that '--verbose' should be treated instead as an argument for
  dotnet-run.
"
            };

            app.HelpOption("-?|-h|--help");

            CommandOption optProject = app.Option("-p|--project <PROJECT>", "The location of the project directory or project file whose web assets needs to be bundled.",
                CommandOptionType.SingleValue);

            CommandOption optBuildConfiguration = app.Option("-c|--configuration <CONFIGURATION>", "The configuration to use for building the project. The default for most projects is 'Debug'.",
                CommandOptionType.SingleValue);

            CommandOption optBuildTargetPath = app.Option("--target-path <TARGET_PATH>", $"The path to the build target. Internal use only.",
                CommandOptionType.SingleValue);
            optBuildTargetPath.ShowInHelpText = false;

            CommandOption optConfigSources = app.Option("-s|--sources <SOURCES>", $"Specifies where to look for bundle configurations. You may specify multiple values separated with comma. Possible values: {app.GetEnumValues<ConfigSources>()}. Default: {nameof(ConfigSources.ConfigFile)},{nameof(ConfigSources.AppAssembly)}.",
                CommandOptionType.SingleValue);

            CommandOption optConfigFile = app.Option("-f|--config-file <CONFIG_FILE>", $"The location of the bundle configuration file. (Ignored when {nameof(ConfigSources.ConfigFile)} source is not enabled by the '--sources' option.) See https://docs.microsoft.com/en-us/aspnet/core/client-side/bundling-and-minification#configure-bundling-and-minification",
                CommandOptionType.SingleValue);

            CommandOption optMode = app.Option("-m|--mode <MODE>", $"Enable production optimizations or development hints. Possible values: {app.GetEnumValues<BundlingMode>()}. Default: {nameof(BundlingMode.Production)}.",
                CommandOptionType.SingleValue);

            CommandOption optQuiet = app.Option("-q|--quiet", "Suppresses all output except warnings and errors",
                CommandOptionType.NoValue);
            CommandOption optVerbose = app.VerboseOption();

            app.VersionOptionFromAssemblyAttributes(typeof(Program).GetTypeInfo().Assembly);

            if (app.Execute(args) != 0)
                return null;

            if (optQuiet.HasValue() && optVerbose.HasValue())
                throw new CommandParsingException(app, "Cannot specify both '--quiet' and '--verbose' options.");

            if (!optConfigSources.TryParse(ConfigSources.Default, out ConfigSources configSources))
                throw new CommandParsingException(app, $"Value is invalid for the '{"--sources"}' option.");

            if (!optMode.TryParse(BundlingMode.Production, out BundlingMode mode))
                throw new CommandParsingException(app, $"Value is invalid for the '{"--mode"}' option.");

            return new CommandLineOptions
            {
                Project = optProject.Value(),
                BuildConfiguration = optBuildConfiguration.Value(),
                BuildTargetPath = optBuildTargetPath.Value(),
                ConfigSources = configSources,
                ConfigFile = optConfigFile.Value(),
                Mode = mode,
                IsQuiet = optQuiet.HasValue(),
                IsVerbose = optVerbose.HasValue(),
                RemainingArguments = app.RemainingArguments,
                IsHelp = app.IsShowingInformation,
            };
        }
    }
}
