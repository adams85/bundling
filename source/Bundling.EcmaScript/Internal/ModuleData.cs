using System.Collections.Generic;
using Esprima.Ast;

namespace Karambolo.AspNetCore.Bundling.EcmaScript.Internal
{
    internal partial class ModuleBundler
    {
        private class ModuleData
        {
            public ModuleData(ModuleFile file)
            {
                File = file;
            }

            public ModuleFile File { get; }
            public string FilePath => File.FilePath;
            public string Content
            {
                get => File.Content;
                set => File.Content = value;
            }
            public Program Ast { get; set; }
            public Dictionary<ModuleFile, string> ModuleRefs { get; set; }
            public List<ExportData> ExportsRaw { get; set; }
            public Dictionary<string, ImportData> Imports { get; set; }
        }
    }
}
