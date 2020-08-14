using System.Collections.Generic;
using Karambolo.AspNetCore.Bundling.Internal;

namespace Karambolo.AspNetCore.Bundling.EcmaScript
{
    public readonly struct ModuleBundlingResult
    {
        public ModuleBundlingResult(string content, ISet<AbstractionFile> imports)
        {
            Content = content;
            Imports = imports;
        }

        public string Content { get; }
        public ISet<AbstractionFile> Imports { get; }
    }
}
