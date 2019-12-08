using System;
using System.Collections.Generic;
using System.Text;

namespace Karambolo.AspNetCore.Bundling.Tools.Infrastructure
{
    internal class ReporterAdapter : IReporter
    {
        public static readonly ReporterAdapter Default = new ReporterAdapter();

        public void Verbose(string message) => Reporter.WriteVerbose(message);
        public void Output(string message) => Reporter.WriteInformation(message);
        public void Warn(string message) => Reporter.WriteWarning(message);
        public void Error(string message) => Reporter.WriteError(message);
    }
}
