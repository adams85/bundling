using System.Collections.Generic;

namespace Karambolo.AspNetCore.Bundling.Less
{
    public readonly struct LessCompilationResult
    {
        public LessCompilationResult(string content, IList<string> imports)
        {
            Content = content;
            Imports = imports;
        }

        public string Content { get; }
        public IList<string> Imports { get; }
    }
}
