using System.Collections.Generic;
using Acornima.Ast;

namespace Karambolo.AspNetCore.Bundling.EcmaScript.Internal
{
    internal partial class ModuleBundler
    {
        internal sealed class ModuleData
        {
            public ModuleData(ModuleResource resource)
            {
                Resource = resource;
            }

            public ModuleResource Resource { get; }

            public string Content { get; set; }

            public Program Ast { get; set; }
            public Dictionary<ModuleResource, string> ModuleRefs { get; set; }
            public List<ExportData> ExportsRaw { get; set; }
            public Dictionary<string, ImportData> Imports { get; set; }
            public bool UsesImportMeta { get; set; }
            public bool RequiresDefine => UsesImportMeta;
        }
    }
}
