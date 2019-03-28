using System;
using System.Collections.Generic;

namespace Karambolo.AspNetCore.Bundling.Sass
{
    public readonly struct SassCompilationResult
    {
        public static readonly SassCompilationResult Failure = default;

        public SassCompilationResult(string content, IList<string> imports)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));

            Content = content;
            Imports = imports;
        }

        public string Content { get; }
        public IList<string> Imports { get; }

        public bool Success => Content != null;
    }
}
