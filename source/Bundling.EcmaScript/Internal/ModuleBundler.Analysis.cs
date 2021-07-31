using Esprima.Ast;
using Karambolo.AspNetCore.Bundling.EcmaScript.Internal.Helpers;
using Karambolo.AspNetCore.Bundling.Internal.Helpers;
using Microsoft.Extensions.Primitives;

namespace Karambolo.AspNetCore.Bundling.EcmaScript.Internal
{
    internal partial class ModuleBundler
    {
        private sealed class VariableDeclarationAnalyzer : VariableScopeBuilder
        {
            private readonly ModuleBundler _bundler;
            private readonly ModuleData _module;

            private readonly string _basePath;
            private int _moduleIndex;

            public VariableDeclarationAnalyzer(ModuleBundler bundler, ModuleData module) : base(module.VariableScopes)
            {
                _bundler = bundler;
                _module = module;

                UrlUtils.GetFileNameSegment(module.FilePath, out StringSegment basePathSegment);
                _basePath = basePathSegment.Value;
            }

            public void Analyze() => Visit(_module.Ast);

            protected override VariableScope.GlobalBlock HandleInvalidImportDeclaration(ImportDeclaration importDeclaration, string defaultErrorMessage) =>
                throw _bundler._logger.RewritingModuleFileFailed(_module.FilePath, _bundler.GetFileProviderHint(_module.File), importDeclaration.Location.Start, defaultErrorMessage);

            private void ExtractExports(ExportAllDeclaration exportAllDeclaration)
            {
                var modulePath = NormalizeModulePath(_basePath, exportAllDeclaration.Source.StringValue);
                var moduleRefFile = new ModuleFile(_module.File, modulePath);

                _module.ExportsRaw.Add(new ReexportData(moduleRefFile));

                if (!_module.ModuleRefs.ContainsKey(moduleRefFile))
                    _module.ModuleRefs[moduleRefFile] = GetModuleName(_moduleIndex++);
            }

            private void ExtractExports(ExportDefaultDeclaration exportDefaultDeclaration)
            {
                switch (exportDefaultDeclaration.Declaration)
                {
                    // export default function myFunc() {}
                    case FunctionDeclaration functionDeclaration when functionDeclaration.Id != null:
                        _module.ExportsRaw.Add(new NamedExportData(DefaultExportName, functionDeclaration.Id.Name));
                        break;
                    // export default class MyClass {}
                    case ClassDeclaration classDeclaration when classDeclaration.Id != null:
                        _module.ExportsRaw.Add(new NamedExportData(DefaultExportName, classDeclaration.Id.Name));
                        break;
                    // export default function() { }
                    // export default class { }
                    // export default <expression>;
                    default:
                        _module.ExportsRaw.Add(new NamedExportData(DefaultExportName, DefaultExportId));
                        break;
                }
            }

            private void ExtractExports(ExportNamedDeclaration exportNamedDeclaration)
            {
                if (exportNamedDeclaration.Declaration == null)
                {
                    // export { a as alias };
                    if (exportNamedDeclaration.Source == null)
                    {
                        for (int i = 0, n = exportNamedDeclaration.Specifiers.Count; i < n; i++)
                        {
                            ExportSpecifier exportSpecifier = exportNamedDeclaration.Specifiers[i];
                            _module.ExportsRaw.Add(new NamedExportData(exportSpecifier.Exported.Name, exportSpecifier.Local.Name));
                        }
                    }
                    // export { default as defaultAlias, a as alias, b } from 'bar.js';
                    else
                    {
                        string modulePath;
                        ModuleFile moduleRefFile;

                        for (int i = 0, n = exportNamedDeclaration.Specifiers.Count; i < n; i++)
                        {
                            ExportSpecifier exportSpecifier = exportNamedDeclaration.Specifiers[i];

                            modulePath = NormalizeModulePath(_basePath, exportNamedDeclaration.Source.StringValue);
                            moduleRefFile = new ModuleFile(_module.File, modulePath);

                            _module.ExportsRaw.Add(new ReexportData(moduleRefFile, exportSpecifier.Exported.Name, exportSpecifier.Local.Name));
                        }

                        modulePath = NormalizeModulePath(_basePath, exportNamedDeclaration.Source.StringValue);
                        moduleRefFile = new ModuleFile(_module.File, modulePath);

                        if (!_module.ModuleRefs.ContainsKey(moduleRefFile))
                            _module.ModuleRefs[moduleRefFile] = GetModuleName(_moduleIndex++);
                    }
                }
                // export var { a: alias, b } = { a: 0, b: 1 };
                else
                {
                    switch (exportNamedDeclaration.Declaration)
                    {
                        case VariableDeclaration variableDeclaration:
                            var variableDeclarationVisitor = new VariableDeclarationVisitor<ModuleData>(_module,
                                visitVariableIdentifier: (m, identifier) => m.ExportsRaw.Add(new NamedExportData(identifier.Name)));

                            for (int i = 0, n = variableDeclaration.Declarations.Count; i < n; i++)
                                variableDeclarationVisitor.VisitId(variableDeclaration.Declarations[i]);
                            break;
                        case FunctionDeclaration functionDeclaration:
                            _module.ExportsRaw.Add(new NamedExportData(functionDeclaration.Id.Name));
                            break;
                        case ClassDeclaration classDeclaration:
                            _module.ExportsRaw.Add(new NamedExportData(classDeclaration.Id.Name));
                            break;
                    }
                }
            }

            private void ExtractImports(ImportDeclaration importDeclaration)
            {
                var modulePath = NormalizeModulePath(_basePath, importDeclaration.Source.StringValue);
                var moduleRefFile = new ModuleFile(_module.File, modulePath);

                for (int i = 0, n = importDeclaration.Specifiers.Count; i < n; i++)
                    switch (importDeclaration.Specifiers[i])
                    {
                        //  import localName from 'src/my_lib';
                        case ImportDefaultSpecifier importDefaultSpecifier:
                            _module.Imports[importDefaultSpecifier.Local.Name] = new NamedImportData(moduleRefFile, importDefaultSpecifier.Local.Name, DefaultExportName);
                            break;
                        // import * as my_lib from 'src/my_lib';
                        case ImportNamespaceSpecifier importNamespaceSpecifier:
                            _module.Imports[importNamespaceSpecifier.Local.Name] = new NamespaceImportData(moduleRefFile, importNamespaceSpecifier.Local.Name);
                            break;
                        // import { name1 } from 'src/my_lib';
                        // import { default as foo } from 'src/my_lib';
                        case ImportSpecifier importSpecifier:
                            _module.Imports[importSpecifier.Local.Name] = new NamedImportData(moduleRefFile, importSpecifier.Local.Name, importSpecifier.Imported.Name);
                            break;
                    }

                if (!_module.ModuleRefs.ContainsKey(moduleRefFile))
                    _module.ModuleRefs[moduleRefFile] = GetModuleName(_moduleIndex++);
            }

            protected override void VisitExportAllDeclaration(ExportAllDeclaration exportAllDeclaration)
            {
                base.VisitExportAllDeclaration(exportAllDeclaration);

                ExtractExports(exportAllDeclaration);
            }

            protected override void VisitExportDefaultDeclaration(ExportDefaultDeclaration exportDefaultDeclaration)
            {
                base.VisitExportDefaultDeclaration(exportDefaultDeclaration);

                ExtractExports(exportDefaultDeclaration);
            }

            protected override void VisitExportNamedDeclaration(ExportNamedDeclaration exportNamedDeclaration)
            {
                base.VisitExportNamedDeclaration(exportNamedDeclaration);

                ExtractExports(exportNamedDeclaration);
            }

            protected override void VisitImportDeclaration(ImportDeclaration importDeclaration)
            {
                base.VisitImportDeclaration(importDeclaration);

                ExtractImports(importDeclaration);
            }

            protected override void VisitMetaProperty(MetaProperty metaProperty)
            {
                _module.UsesImportMeta = true;
            }
        }

        private void AnalyzeDeclarations(ModuleData module) => new VariableDeclarationAnalyzer(this, module).Analyze();
    }
}
