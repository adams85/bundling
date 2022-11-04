namespace Karambolo.AspNetCore.Bundling.EcmaScript.Internal
{
    internal partial class ModuleBundler
    {
        internal abstract class ImportData
        {
            protected ImportData(ModuleResource source, string localName)
            {
                Source = source;
                LocalName = localName;
            }

            public ModuleResource Source { get; }
            public string LocalName { get; }
        }

        internal sealed class NamespaceImportData : ImportData
        {
            public NamespaceImportData(ModuleResource source, string localName) : base(source, localName) { }
        }

        internal sealed class NamedImportData : ImportData
        {
            public NamedImportData(ModuleResource source, string localName, ExportName importName) : base(source, localName)
            {
                ImportName = importName;
            }

            public ExportName ImportName { get; }
        }
    }
}
