using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Karambolo.AspNetCore.Bundling.Tools.Infrastructure;
using Microsoft.DotNet.Cli.CommandLine;

namespace Karambolo.AspNetCore.Bundling.Tools
{
    internal abstract class CommandBase
    {
        protected CommandLineApplication Command { get; private set; }

        public virtual void Configure(CommandLineApplication command)
        {
            var verbose = command.Option("-v|--verbose", "Show verbose output.");
            var noColor = command.Option("--no-color", "Don't colorize output.");
            var prefixOutput = command.Option("--prefix-output", "Prefix output with level.");

            command.HandleResponseFiles = true;

            var cancellationToken =
                command is CancelableCommandLineApplication cancelableCommand ?
                cancelableCommand.CancellationToken :
                default;

            command.OnExecute(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                Reporter.IsVerbose = verbose.HasValue();
                Reporter.NoColor = noColor.HasValue();
                Reporter.PrefixOutput = prefixOutput.HasValue();

                Validate();

                return ExecuteAsync(cancellationToken).GetAwaiter().GetResult();
            });

            Command = command;
        }

        protected virtual void Validate() { }

        protected virtual Task<int> ExecuteAsync(CancellationToken cancellationToken) => Task.FromResult(0);
    }
}
