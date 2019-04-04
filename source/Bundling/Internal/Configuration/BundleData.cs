using System.Collections.Generic;
using Newtonsoft.Json;

namespace Karambolo.AspNetCore.Bundling.Internal.Configuration
{
    public class BundleData
    {
        [JsonProperty("outputFileName")]
        public string OutputFileName { get; set; }

        [JsonProperty("inputFiles")]
        public List<string> InputFiles { get; } = new List<string>();

        [JsonProperty("minify")]
        public Dictionary<string, object> Minify { get; } = new Dictionary<string, object> { ["enabled"] = true };

        [JsonProperty("includeInProject")]
        public bool IncludeInProject { get; set; } = true;

        [JsonProperty("sourceMap")]
        public bool SourceMap { get; set; }

        [JsonProperty("sourceMapRootPath")]
        public string SourceMapRootPath { get; set; }
    }
}
