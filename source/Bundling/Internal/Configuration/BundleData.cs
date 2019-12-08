using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Karambolo.AspNetCore.Bundling.Internal.Configuration
{
    public class BundleData
    {
        [JsonPropertyName("outputFileName")]
        public string OutputFileName { get; set; }

        [JsonPropertyName("inputFiles")]
        public List<string> InputFiles { get; set; }

        [JsonPropertyName("minify")]
        public Dictionary<string, object> Minify { get; set; }

        [JsonPropertyName("includeInProject")]
        public bool IncludeInProject { get; set; } = true;

        [JsonPropertyName("sourceMap")]
        public bool SourceMap { get; set; }

        [JsonPropertyName("sourceMapRootPath")]
        public string SourceMapRootPath { get; set; }
    }
}
