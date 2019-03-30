using System.Collections.Generic;
using System.IO;

namespace Karambolo.AspNetCore.Bundling.Tools.Infrastructure
{
    public class ProcessSpec
    {
        public string Executable { get; set; }
        public string WorkingDirectory { get; set; }
        public IDictionary<string, string> EnvironmentVariables { get; } = new Dictionary<string, string>();
        public IEnumerable<string> Arguments { get; set; }
        public OutputCapture OutputCapture { get; set; }

        public string ShortDisplayName()
            => Path.GetFileNameWithoutExtension(Executable);

        public bool IsOutputCaptured => OutputCapture != null;
    }
}
