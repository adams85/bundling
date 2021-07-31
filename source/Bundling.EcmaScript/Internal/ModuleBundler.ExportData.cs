namespace Karambolo.AspNetCore.Bundling.EcmaScript.Internal
{
    internal partial class ModuleBundler
    {
        internal abstract class ExportData
        {
            public ExportData(string exportName)
            {
                ExportName = exportName;
            }

            public string ExportName { get; }
        }

        internal class NamedExportData : ExportData
        {
            public NamedExportData(string localName) : this(localName, localName) { }

            public NamedExportData(string exportName, string localName) : base(exportName)
            {
                LocalName = localName;
            }

            public string LocalName { get; }
        }

        internal class ReexportData : NamedExportData
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
