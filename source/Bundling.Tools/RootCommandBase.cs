using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Karambolo.AspNetCore.Bundling.Tools.Infrastructure;
using Microsoft.DotNet.Cli.CommandLine;

namespace Karambolo.AspNetCore.Bundling.Tools
{
    internal abstract class RootCommandBase : CommandBase
    {
        private CommandOption _sourcesOption;
        private CommandOption _configFileOption;
        private CommandOption _modeOption;
        
        private ConfigSources _configSources;
        protected ConfigSources ConfigSources => _configSources;

        protected string ConfigFilePath => _configFileOption.Value();

        private BundlingMode _bundlingMode;
        protected BundlingMode BundlingMode => _bundlingMode;

        protected abstract void ConfigureCore(CommandLineApplication command);

        public sealed override void Configure(CommandLineApplication command)
        {
            command.FullName = $".NET Core CLI tools for the Karambolo.AspNetCore.Bundling library";

            _sourcesOption = command.Option("-s|--sources <SOURCES>", $"Specifies where to look for bundle configurations. You may specify multiple values separated with comma. Possible values: {command.GetEnumValues<ConfigSources>()}. Default: {nameof(ConfigSources.ConfigFile)},{nameof(ConfigSources.AppAssembly)}.",
                CommandOptionType.SingleValue);

            _configFileOption = command.Option("-f|--config-file <CONFIG_FILE>", $"The location of the bundle configuration file. (Ignored when {nameof(ConfigSources.ConfigFile)} source is not enabled by the '--sources' option.) See https://docs.microsoft.com/en-us/aspnet/core/client-side/bundling-and-minification#configure-bundling-and-minification",
                CommandOptionType.SingleValue);

            _modeOption = command.Option("-m|--mode <MODE>", $"Enable production optimizations or development hints. Possible values: {command.GetEnumValues<BundlingMode>()}. Default: {nameof(BundlingMode.Production)}.",
                CommandOptionType.SingleValue);

            ConfigureCore(command);

            base.Configure(command);
        }

        protected override void Validate()
        {
            base.Validate();

            if (!_sourcesOption.TryParse(ConfigSources.Default, out _configSources))
                throw new CommandParsingException(Command, $"Value is invalid for the '{"--sources"}' option.");

            if (!_modeOption.TryParse(BundlingMode.Production, out _bundlingMode))
                throw new CommandParsingException(Command, $"Value is invalid for the '{"--mode"}' option.");
        }
    }
}
