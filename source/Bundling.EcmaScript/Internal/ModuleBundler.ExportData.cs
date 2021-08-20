namespace Karambolo.AspNetCore.Bundling.EcmaScript.Internal
{
    internal partial class ModuleBundler
    {
        internal abstract class ExportData
        {
            public ExportData(string exportName, string localName)
            {
                ExportName = exportName;
                LocalName = localName;
            }

            public string ExportName { get; }
            public string LocalName { get; }
        }

        internal sealed class NamedExportData : ExportData
        {
            public NamedExportData(string localName) : this(localName, localName) { }

            public NamedExportData(string exportName, string localName) : base(exportName, localName) { }
        }

        internal sealed class ReexportData : ExportData
        {
            public ReexportData(ModuleFile moduleFile) : this(moduleFile, null, null) { }

            public ReexportData(ModuleFile moduleFile, string exportName, string localName) : base(exportName, localName)
            {
                ModuleFile = moduleFile;
            }

            public ModuleFile ModuleFile { get; }
        }
    }
}
