using System.IO;
using Karambolo.AspNetCore.Bundling.Internal.Helpers;

namespace SourcemapToolkit.SourcemapParser
{
    internal class SourceMapParser
    {
        private readonly MappingsListParser _mappingsListParser;

        public SourceMapParser()
        {
            _mappingsListParser = new MappingsListParser();
        }

        /// <summary>
        /// Parses a stream representing a source map into a SourceMap object.
        /// </summary>
        public SourceMap ParseSourceMap(string content)
        {
            if (content == null)
                return null;

            SourceMap result;

            using (var reader = new StringReader(content))
                result = SerializationHelper.Deserialize<SourceMap>(reader);
            
            result.ParsedMappings = _mappingsListParser.ParseMappings(result.Mappings, result.Names, result.Sources);

            return result;
        }
    }
}
