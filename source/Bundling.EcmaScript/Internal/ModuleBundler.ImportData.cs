namespace Karambolo.AspNetCore.Bundling.EcmaScript.Internal
{
    internal partial class ModuleBundler
    {
        internal abstract class ImportData
        {
            protected ImportData(IModuleResource source, string localName)
            {
                Source = source;
                LocalName = localName;
            }

            public IModuleResource Source { get; }
            public string LocalName { get; }
        }

        internal sealed class NamespaceImportData : ImportData
        {
            public NamespaceImportData(IModuleResource source, string localName) : base(source, localName) { }
        }

        internal sealed class NamedImportData : ImportData
        {
            public NamedImportData(IModuleResource source, string localName, ExportName importName) : base(source, localName)
            {
                ImportName = importName;
            }

            public ExportName ImportName { get; }
        }
    }
}
