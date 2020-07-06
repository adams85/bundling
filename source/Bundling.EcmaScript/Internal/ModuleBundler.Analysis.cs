using Esprima.Ast;

namespace Karambolo.AspNetCore.Bundling.EcmaScript.Internal
{
    internal partial class ModuleBundler
    {
        private void ExtractVariableExports(ModuleData module, RestElement restElement)
        {
            switch (restElement.Argument)
            {
                case Identifier identifier:
                    module.ExportsRaw.Add(new NamedExportData(identifier.Name));
                    break;
                case ArrayPattern arrayPattern:
                    ExtractVariableExports(module, arrayPattern);
                    break;
                case ObjectPattern objectPattern:
                    ExtractVariableExports(module, objectPattern);
                    break;
            }
        }

        private void ExtractVariableExports(ModuleData module, ArrayPattern arrayPattern)
        {
            for (int i = 0, n = arrayPattern.Elements.Count; i < n; i++)
                switch (arrayPattern.Elements[i])
                {
                    case Identifier identifier:
                        module.ExportsRaw.Add(new NamedExportData(identifier.Name));
                        break;
                    case ArrayPattern nestedArrayPattern:
                        ExtractVariableExports(module, nestedArrayPattern);
                        break;
                    case ObjectPattern nestedObjectPattern:
                        ExtractVariableExports(module, nestedObjectPattern);
                        break;
                    case RestElement restElement:
                        ExtractVariableExports(module, restElement);
                        break;
                }
        }

        private void ExtractVariableExports(ModuleData module, ObjectPattern objectPattern)
        {
            for (int i = 0, n = objectPattern.Properties.Count; i < n; i++)
                switch (objectPattern.Properties[i])
                {
                    case Property property:
                        switch (property.Value)
                        {
                            case Identifier identifier:
                                module.ExportsRaw.Add(new NamedExportData(identifier.Name));
                                break;
                            case ArrayPattern nestedArrayPattern:
                                ExtractVariableExports(module, nestedArrayPattern);
                                break;
                            case ObjectPattern nestedObjectPattern:
                                ExtractVariableExports(module, nestedObjectPattern);
                                break;
                        }
                        break;
                    case RestElement restElement:
                        ExtractVariableExports(module, restElement);
                        break;
                }
        }

        private void ExtractExports(ModuleData module, IStatementListItem exportDeclaration)
        {
            switch (exportDeclaration)
            {
                case VariableDeclaration variableDeclaration:
                    for (int i = 0, n = variableDeclaration.Declarations.Count; i < n; i++)
                        switch (variableDeclaration.Declarations[i].Id)
                        {
                            case Identifier identifier:
                                module.ExportsRaw.Add(new NamedExportData(identifier.Name));
                                break;
                            case ArrayPattern arrayPattern:
                                ExtractVariableExports(module, arrayPattern);
                                break;
                            case ObjectPattern objectPattern:
                                ExtractVariableExports(module, objectPattern);
                                break;
                        }
                    break;
                case FunctionDeclaration functionDeclaration:
                    module.ExportsRaw.Add(new NamedExportData(functionDeclaration.Id.Name));
                    break;
                case ClassDeclaration classDeclaration:
                    module.ExportsRaw.Add(new NamedExportData(classDeclaration.Id.Name));
                    break;
            }
        }

        private void ExtractExports(ModuleData module, NodeList<ExportSpecifier> exportSpecifiers)
        {
            for (int i = 0, n = exportSpecifiers.Count; i < n; i++)
            {
                ExportSpecifier exportSpecifier = exportSpecifiers[i];
                module.ExportsRaw.Add(new NamedExportData(exportSpecifier.Exported.Name, exportSpecifier.Local.Name));
            }
        }

        private void ExtractExports(ModuleData module, ExportDefaultDeclaration defaultDeclaration)
        {
            switch (defaultDeclaration.Declaration)
            {
                case FunctionDeclaration functionDeclaration when functionDeclaration.Id != null:
                    // export default function myFunc() {}
                    module.ExportsRaw.Add(new NamedExportData(DefaultExportName, functionDeclaration.Id.Name));
                    break;
                case ClassDeclaration classDeclaration when classDeclaration.Id != null:
                    // export default class MyClass {}
                    module.ExportsRaw.Add(new NamedExportData(DefaultExportName, classDeclaration.Id.Name));
                    break;
                default:
                    // export default function() { }
                    // export default class { }
                    // export default <expression>;
                    module.ExportsRaw.Add(new DefaultExpressionExportData(defaultDeclaration.Declaration));
                    break;
            }
        }

        private void ExtractExports(ModuleData module, ExportNamedDeclaration namedDeclaration)
        {
            // export var { a: alias, b } = { a: 0, b: 1 };
            if (namedDeclaration.Declaration != null)
                ExtractExports(module, namedDeclaration.Declaration);
            // export { a as alias };
            else
                ExtractExports(module, namedDeclaration.Specifiers);
        }

        private void ExtractImports(ModuleData module, ImportDeclaration importDeclaration, ModuleFile moduleFile)
        {
            for (int i = 0, n = importDeclaration.Specifiers.Count; i < n; i++)
                switch (importDeclaration.Specifiers[i])
                {
                    //  import localName from 'src/my_lib';
                    case ImportDefaultSpecifier importDefaultSpecifier:
                        module.Imports[importDefaultSpecifier.Local.Name] = new NamedImportData(moduleFile, importDefaultSpecifier.Local.Name, DefaultExportName);
                        break;
                    // import * as my_lib from 'src/my_lib';
                    case ImportNamespaceSpecifier importNamespaceSpecifier:
                        module.Imports[importNamespaceSpecifier.Local.Name] = new NamespaceImportData(moduleFile, importNamespaceSpecifier.Local.Name);
                        break;
                    // import { name1 } from 'src/my_lib';
                    // import { default as foo } from 'src/my_lib';
                    case ImportSpecifier importSpecifier:
                        module.Imports[importSpecifier.Local.Name] = new NamedImportData(moduleFile, importSpecifier.Local.Name, importSpecifier.Imported.Name);
                        break;
                }
        }

        private void AnalyzeDeclarations(ModuleData module)
        {
            var index = module.FilePath.LastIndexOf('/');
            var basePath = index >= 0 ? module.FilePath.Substring(0, index + 1) : "/";
            string modulePath;

            ModuleFile moduleRefFile;
            index = 0; // variable re-used as module counter

            for (int i = 0, n = module.Ast.Body.Count; i < n; i++)
                switch (module.Ast.Body[i])
                {
                    case ExportAllDeclaration exportAllDeclaration:
                        modulePath = NormalizeModulePath(basePath, exportAllDeclaration.Source.StringValue);
                        moduleRefFile = new ModuleFile(module.File, modulePath);

                        module.ExportsRaw.Add(new ReexportData(moduleRefFile));

                        if (!module.ModuleRefs.ContainsKey(moduleRefFile))
                            module.ModuleRefs[moduleRefFile] = GetModuleName(index++);
                        break;
                    case ExportDefaultDeclaration exportDefaultDeclaration:
                        ExtractExports(module, exportDefaultDeclaration);
                        break;
                    case ExportNamedDeclaration exportNamedDeclaration:
                        if (exportNamedDeclaration.Source != null)
                        {
                            for (int j = 0, m = exportNamedDeclaration.Specifiers.Count; j < m; j++)
                            {
                                ExportSpecifier exportSpecifier = exportNamedDeclaration.Specifiers[j];

                                modulePath = NormalizeModulePath(basePath, exportNamedDeclaration.Source.StringValue);
                                moduleRefFile = new ModuleFile(module.File, modulePath);

                                module.ExportsRaw.Add(new ReexportData(moduleRefFile, exportSpecifier.Exported.Name, exportSpecifier.Local.Name));
                            }

                            modulePath = NormalizeModulePath(basePath, exportNamedDeclaration.Source.StringValue);
                            moduleRefFile = new ModuleFile(module.File, modulePath);

                            if (!module.ModuleRefs.ContainsKey(moduleRefFile))
                                module.ModuleRefs[moduleRefFile] = GetModuleName(index++);
                        }
                        else
                            ExtractExports(module, exportNamedDeclaration);
                        break;
                    case ImportDeclaration importDeclaration:
                        modulePath = NormalizeModulePath(basePath, importDeclaration.Source.StringValue);
                        moduleRefFile = new ModuleFile(module.File, modulePath);

                        ExtractImports(module, importDeclaration, moduleRefFile);

                        if (!module.ModuleRefs.ContainsKey(moduleRefFile))
                            module.ModuleRefs[moduleRefFile] = GetModuleName(index++);
                        break;
                }
        }
    }
}
