using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.DotNet.Cli.CommandLine;

namespace Karambolo.AspNetCore.Bundling.Tools
{
    internal class CancelableCommandLineApplication : CommandLineApplication
    {
        public CancelableCommandLineApplication(bool throwOnUnexpectedArg = true, CancellationToken cancellationToken = default)
            : base(throwOnUnexpectedArg)
        {
            CancellationToken = cancellationToken;
        }

        public CancellationToken CancellationToken { get; }
    }
}
