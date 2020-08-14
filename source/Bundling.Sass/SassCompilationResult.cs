using System.Collections.Generic;

namespace Karambolo.AspNetCore.Bundling.Sass
{
    public readonly struct SassCompilationResult
    {
        public SassCompilationResult(string content, IList<string> imports)
        {
            Content = content;
            Imports = imports;
        }

        public string Content { get; }
        public IList<string> Imports { get; }
    }
}
