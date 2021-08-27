using System.Collections.Generic;
using Esprima;
using Esprima.Ast;
using Karambolo.AspNetCore.Bundling.EcmaScript.Internal.Helpers;

namespace Karambolo.AspNetCore.Bundling.EcmaScript.Internal
{
    internal partial class ModuleBundler
    {
        internal sealed class ModuleData
        {
            public ModuleData(IModuleResource resource)
            {
                Resource = resource;
            }

            public IModuleResource Resource { get; }

            public string Content { get; set; }

            public ParserOptions ParserOptions { get; set; }
            public Program Ast { get; set; }
            public Dictionary<Node, VariableScope> VariableScopes { get; set; }
            public Dictionary<IModuleResource, string> ModuleRefs { get; set; }
            public List<ExportData> ExportsRaw { get; set; }
            public Dictionary<string, ImportData> Imports { get; set; }
            public bool UsesImportMeta { get; set; }
        }
    }
}
