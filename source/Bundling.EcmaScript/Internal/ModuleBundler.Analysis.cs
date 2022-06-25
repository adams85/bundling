using System;
using Esprima;
using Esprima.Ast;
using Karambolo.AspNetCore.Bundling.EcmaScript.Internal.Helpers;

namespace Karambolo.AspNetCore.Bundling.EcmaScript.Internal
{
    // TODO: (support import, export name literals) + TEST!
    internal partial class ModuleBundler
    {
        private sealed class VariableDeclarationAnalyzer : VariableScopeBuilder
        {
            private readonly ModuleBundler _bundler;
            private readonly ModuleData _module;

            private int _moduleIndex;

            public VariableDeclarationAnalyzer(ModuleBundler bundler, ModuleData module)
            {
                _bundler = bundler;
                _module = module;
            }

            public void Analyze() => Visit(_module.Ast);

            protected override VariableScope.GlobalBlock HandleInvalidImportDeclaration(ImportDeclaration importDeclaration, string defaultErrorMessage) =>
                throw _bundler._logger.RewritingModuleFailed(_module.Resource.Url.ToString(), importDeclaration.Location.Start, defaultErrorMessage);

            private IModuleResource ResolveImportSource(string url)
            {
                return _module.Resource.Resolve(url, this, (analyzer, sourceUrl, reason) =>
                    throw analyzer._bundler._logger.ResolvingImportSourceFailed(analyzer._module.Resource.Url.ToString(), sourceUrl, reason));
            }

            private Exception InvalidExportImportNameExpression(Expression expression)
            {
                throw _bundler._logger.RewritingModuleFailed(_module.Resource.Url.ToString(), expression.Location.Start,
                    $"Expression of type {expression.Type} is not a valid export/import name expression.");
            }

            private string GetExportImportName(Expression expression)
            {
                return
                (
                    expression is Identifier identifier ? identifier.Name :
                    expression is Literal literal && literal.TokenType == TokenType.StringLiteral ? literal.StringValue :
                    null
                ) ?? throw InvalidExportImportNameExpression(expression);
            }

            private string GetLocalName(Expression expression)
            {
                return
                (
                    expression is Identifier identifier ? identifier.Name :
                    null
                ) ?? throw InvalidExportImportNameExpression(expression);
            }

            private void ExtractExports(ExportAllDeclaration exportAllDeclaration)
            {
                if (exportAllDeclaration.Exported != null)
                {
                    // TODO: support re-export all as namespace (ExportAllDeclaration.Exported)
                    throw new NotImplementedException();
                }

                IModuleResource source = ResolveImportSource(exportAllDeclaration.Source.StringValue);

                _module.ExportsRaw.Add(new ExportAllData(source));

                if (!_module.ModuleRefs.ContainsKey(source))
                    _module.ModuleRefs[source] = GetModuleRef(_moduleIndex++);
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
                        ref readonly NodeList<ExportSpecifier> specifiers = ref exportNamedDeclaration.Specifiers;
                        for (var i = 0; i < specifiers.Count; i++)
                        {
                            ExportSpecifier exportSpecifier = specifiers[i];
                            _module.ExportsRaw.Add(new NamedExportData(GetExportImportName(exportSpecifier.Exported), GetLocalName(exportSpecifier.Local)));
                        }
                    }
                    // export { default as defaultAlias, a as alias, b } from 'bar.js';
                    else
                    {
                        IModuleResource source = ResolveImportSource(exportNamedDeclaration.Source.StringValue);

                        ref readonly NodeList<ExportSpecifier> specifiers = ref exportNamedDeclaration.Specifiers;
                        for (var i = 0; i < specifiers.Count; i++)
                        {
                            ExportSpecifier exportSpecifier = specifiers[i];

                            _module.ExportsRaw.Add(new ReexportData(source, GetExportImportName(exportSpecifier.Exported), GetExportImportName(exportSpecifier.Local)));
                        }

                        if (!_module.ModuleRefs.ContainsKey(source))
                            _module.ModuleRefs[source] = GetModuleRef(_moduleIndex++);
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

                            ref readonly NodeList<VariableDeclarator> declarations = ref variableDeclaration.Declarations;
                            for (var i = 0; i < declarations.Count; i++)
                                variableDeclarationVisitor.VisitId(declarations[i]);
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
                IModuleResource source = ResolveImportSource(importDeclaration.Source.StringValue);

                ref readonly NodeList<ImportDeclarationSpecifier> specifiers = ref importDeclaration.Specifiers;
                for (var i = 0; i < specifiers.Count; i++)
                    switch (specifiers[i])
                    {
                        //  import localName from 'src/my_lib';
                        case ImportDefaultSpecifier importDefaultSpecifier:
                            _module.Imports[importDefaultSpecifier.Local.Name] = new NamedImportData(source, importDefaultSpecifier.Local.Name, DefaultExportName);
                            break;
                        // import * as my_lib from 'src/my_lib';
                        case ImportNamespaceSpecifier importNamespaceSpecifier:
                            _module.Imports[importNamespaceSpecifier.Local.Name] = new NamespaceImportData(source, importNamespaceSpecifier.Local.Name);
                            break;
                        // import { name1 } from 'src/my_lib';
                        // import { default as foo } from 'src/my_lib';
                        case ImportSpecifier importSpecifier:
                            _module.Imports[importSpecifier.Local.Name] = new NamedImportData(source, importSpecifier.Local.Name, GetExportImportName(importSpecifier.Imported));
                            break;
                    }

                if (!_module.ModuleRefs.ContainsKey(source))
                    _module.ModuleRefs[source] = GetModuleRef(_moduleIndex++);
            }

            protected override object VisitExportAllDeclaration(ExportAllDeclaration exportAllDeclaration)
            {
                base.VisitExportAllDeclaration(exportAllDeclaration);

                ExtractExports(exportAllDeclaration);

                return exportAllDeclaration;
            }

            protected override object VisitExportDefaultDeclaration(ExportDefaultDeclaration exportDefaultDeclaration)
            {
                base.VisitExportDefaultDeclaration(exportDefaultDeclaration);

                ExtractExports(exportDefaultDeclaration);

                return exportDefaultDeclaration;
            }

            protected override object VisitExportNamedDeclaration(ExportNamedDeclaration exportNamedDeclaration)
            {
                base.VisitExportNamedDeclaration(exportNamedDeclaration);

                ExtractExports(exportNamedDeclaration);

                return exportNamedDeclaration;
            }

            internal static bool IsRewritableDynamicImport(Import import, out Literal sourceLiteral)
            {
                if (import.Source is Literal literal && literal.TokenType == TokenType.StringLiteral && literal.Value != null)
                {
                    sourceLiteral = literal;
                    return true;
                }

                sourceLiteral = default;
                return false;
            }

            protected override object VisitImport(Import import)
            {
                if (IsRewritableDynamicImport(import, out Literal sourceLiteral))
                {
                    IModuleResource source = ResolveImportSource(sourceLiteral.StringValue);

                    if (!_module.ModuleRefs.ContainsKey(source))
                        _module.ModuleRefs[source] = GetModuleRef(_moduleIndex++);
                }
                else
                    _bundler._logger.NonRewritableDynamicImportWarning(_module.Resource.Url.ToString(), import.Location.Start);

                return import;
            }

            protected override object VisitImportDeclaration(ImportDeclaration importDeclaration)
            {
                base.VisitImportDeclaration(importDeclaration);

                ExtractImports(importDeclaration);

                return importDeclaration;
            }

            protected override object VisitMetaProperty(MetaProperty metaProperty)
            {
                _module.UsesImportMeta = true;

                return metaProperty;
            }
        }

        private void AnalyzeDeclarations(ModuleData module) => new VariableDeclarationAnalyzer(this, module).Analyze();
    }
}
