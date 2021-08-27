namespace Karambolo.AspNetCore.Bundling.EcmaScript.Internal
{
    internal partial class ModuleBundler
    {
        internal abstract class ExportData
        {
            protected ExportData(string exportName)
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

        internal sealed class ReexportData : NamedExportData
        {
            public ReexportData(IModuleResource source, string exportName, string localName) : base(exportName, localName)
            {
                Source = source;
            }

            public IModuleResource Source { get; }
        }

        internal sealed class ExportAllData : ExportData
        {
            public ExportAllData(IModuleResource source) : base(null)
            {
                Source = source;
            }

            public IModuleResource Source { get; }
        }
    }
}
