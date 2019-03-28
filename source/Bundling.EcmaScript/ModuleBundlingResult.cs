using System;
using System.Collections.Generic;
using Karambolo.AspNetCore.Bundling.Internal;

namespace Karambolo.AspNetCore.Bundling.EcmaScript
{
    public readonly struct ModuleBundlingResult
    {
        public static readonly ModuleBundlingResult Failure = default;

        public ModuleBundlingResult(string content, ISet<AbstractionFile> imports)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));

            Content = content;
            Imports = imports;
        }

        public string Content { get; }
        public ISet<AbstractionFile> Imports { get; }

        public bool Success => Content != null;
    }
}
