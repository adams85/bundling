using Esprima.Ast;

namespace Karambolo.AspNetCore.Bundling.EcmaScript.Internal
{
    internal partial class ModuleBundler
    {
        private abstract class ExportData
        {
            public ExportData(string exportName)
            {
                ExportName = exportName;
            }

            public string ExportName { get; }
        }

        private class NamedExportData : ExportData
        {
            public NamedExportData(string localName) : this(localName, localName) { }

            public NamedExportData(string exportName, string localName) : base(exportName)
            {
                LocalName = localName;
            }

            public string LocalName { get; }
        }

        private class DefaultExpressionExportData : ExportData
        {
            public DefaultExpressionExportData(IDeclaration expression) : base(ModuleBundler.DefaultExportName)
            {
                Expression = expression;
            }

            public IDeclaration Expression { get; }
        }

        private class ReexportData : NamedExportData
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
