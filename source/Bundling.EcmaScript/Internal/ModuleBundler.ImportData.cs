namespace Karambolo.AspNetCore.Bundling.EcmaScript.Internal
{
    internal partial class ModuleBundler
    {
        private abstract class ImportData
        {
            public ImportData(ModuleFile moduleFile, string localName)
            {
                ModuleFile = moduleFile;
                LocalName = localName;
            }

            public ModuleFile ModuleFile { get; }
            public string LocalName { get; }
        }

        private class NamespaceImportData : ImportData
        {
            public NamespaceImportData(ModuleFile moduleFile, string localName) : base(moduleFile, localName) { }
        }

        private class NamedImportData : ImportData
        {
            public NamedImportData(ModuleFile moduleFile, string localName, string importName) : base(moduleFile, localName)
            {
                ImportName = importName;
            }

            public string ImportName { get; }
        }
    }
}
