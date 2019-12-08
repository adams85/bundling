using System;
using System.Threading;
using Karambolo.AspNetCore.Bundling.Tools.Infrastructure;
using Microsoft.DotNet.Cli.CommandLine;

namespace Karambolo.AspNetCore.Bundling.Tools
{
    public class Program
    {
        internal const string NestedRunEnvironmentVariableName = "BUNDLING_NESTED_RUN";

        private static int Main(string[] args)
        {
            var isNestedRun = Environment.GetEnvironmentVariable(NestedRunEnvironmentVariableName) == bool.TrueString;

            using (var cts = new CancellationTokenSource())
            {
                Console.CancelKeyPress += (s, e) => cts.Cancel();

                var app = new CancelableCommandLineApplication(throwOnUnexpectedArg: !isNestedRun, cancellationToken: cts.Token) { Name = "dotnet bundle" };

                var rootCommand =
                    !isNestedRun ?
                    new RootCommand(args) :
                    (RootCommandBase)new RootCommandNested();

                rootCommand.Configure(app);

                try
                {
                    return app.Execute(args);
                }
                catch (OperationCanceledException)
                {
                    // swallow when only exception is the CTRL+C forced an exit
                    return 0;
                }
                catch (Exception ex)
                {
                    if (ex is CommandException || ex is CommandParsingException)
                        Reporter.WriteVerbose(ex.ToString());
                    else
                        Reporter.WriteInformation(ex.ToString());

                    Reporter.WriteError(ex.Message);

                    return 1;
                }
            }
        }
    }
}
