namespace Karambolo.AspNetCore.Bundling.EcmaScript.Internal
{
    internal partial class ModuleBundler
    {
        internal abstract class ExportData
        {
            protected ExportData(ExportName exportName)
            {
                ExportName = exportName;
            }

            public ExportName ExportName { get; }
        }

        internal class NamedExportData : ExportData
        {
            public NamedExportData(string localName) : this(new ExportName(localName), localName) { }

            public NamedExportData(ExportName exportName, string localName) : base(exportName)
            {
                LocalName = localName;
            }

            public string LocalName { get; }
        }

        internal sealed class ReexportData : ExportData
        {
            public ReexportData(ModuleResource source, ExportName exportName, ExportName importName) : base(exportName)
            {
                Source = source;
                ImportName = importName;
            }

            public ModuleResource Source { get; }
            public ExportName ImportName { get; }
        }

        internal sealed class WildcardReexportData : ExportData
        {
            public WildcardReexportData(ModuleResource source, ExportName exportName) : base(exportName)
            {
                Source = source;
            }

            public ModuleResource Source { get; }
        }
    }
}
