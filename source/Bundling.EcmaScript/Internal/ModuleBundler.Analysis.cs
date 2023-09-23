using System;
using Esprima;
using Esprima.Ast;
using Karambolo.AspNetCore.Bundling.EcmaScript.Internal.Helpers;

namespace Karambolo.AspNetCore.Bundling.EcmaScript.Internal
{
    internal partial class ModuleBundler
    {
        internal static bool IsRewritableDynamicImport(ImportExpression importExpression, out Literal sourceLiteral)
        {
            if (importExpression.Source is Literal literal && literal.TokenType == TokenType.StringLiteral && literal.Value != null)
            {
                sourceLiteral = literal;
                return true;
            }

            sourceLiteral = default;
            return false;
        }

        internal static bool IsImportMeta(MetaProperty metaProperty)
        {
            return metaProperty.Meta.Name == "import" && metaProperty.Property.Name == "meta";
        }

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

            protected override VariableScope.TopLevelBlock HandleInvalidImportDeclaration(ImportDeclaration importDeclaration, string defaultErrorMessage) =>
                throw _bundler._logger.RewritingModuleFailed(_module.Resource.Url.ToString(), importDeclaration.Location.Start, defaultErrorMessage);

            private Exception InvalidExportImportNameExpression(Expression expression)
            {
                throw _bundler._logger.RewritingModuleFailed(_module.Resource.Url.ToString(), expression.Location.Start,
                    $"Expression of type {expression.Type} is not a valid export/import name expression.");
            }

            private ExportName GetExportName(Expression expression)
            {
                return
                    expression is Identifier identifier ? new ExportName(identifier.Name) :
                    expression is Literal { TokenType: TokenType.StringLiteral } literal ? new ExportName((string)literal.Value, literal.Raw) :
                    throw InvalidExportImportNameExpression(expression);
            }

            private string GetLocalName(Expression expression)
            {
                return
                    expression is Identifier identifier ? identifier.Name :
                    throw InvalidExportImportNameExpression(expression);
            }

            private void ExtractExports(ExportAllDeclaration exportAllDeclaration)
            {
                ModuleResource source = _bundler.ResolveImport(exportAllDeclaration.Source.StringValue, _module.Resource);

                ExportName exportName = exportAllDeclaration.Exported != null ? GetExportName(exportAllDeclaration.Exported) : ExportName.None;
                _module.ExportsRaw.Add(new WildcardReexportData(source, exportName));

                if (!_module.ModuleRefs.ContainsKey(source))
                    _module.ModuleRefs[source] = GetModuleRef(_moduleIndex++);
            }

            private void ExtractExports(ExportDefaultDeclaration exportDefaultDeclaration)
            {
                switch (exportDefaultDeclaration.Declaration)
                {
                    // export default function myFunc() {}
                    case FunctionDeclaration functionDeclaration when functionDeclaration.Id != null:
                        _module.ExportsRaw.Add(new NamedExportData(ExportName.Default, functionDeclaration.Id.Name));
                        break;
                    // export default class MyClass {}
                    case ClassDeclaration classDeclaration when classDeclaration.Id != null:
                        _module.ExportsRaw.Add(new NamedExportData(ExportName.Default, classDeclaration.Id.Name));
                        break;
                    // export default function() { }
                    // export default class { }
                    // export default <expression>;
                    default:
                        _module.ExportsRaw.Add(new NamedExportData(ExportName.Default, DefaultExportId));
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
                            _module.ExportsRaw.Add(new NamedExportData(GetExportName(exportSpecifier.Exported), GetLocalName(exportSpecifier.Local)));
                        }
                    }
                    // export { default as defaultAlias, a as alias, b } from 'bar.js';
                    else
                    {
                        ModuleResource source = _bundler.ResolveImport(exportNamedDeclaration.Source.StringValue, _module.Resource);

                        ref readonly NodeList<ExportSpecifier> specifiers = ref exportNamedDeclaration.Specifiers;
                        for (var i = 0; i < specifiers.Count; i++)
                        {
                            ExportSpecifier exportSpecifier = specifiers[i];

                            _module.ExportsRaw.Add(new ReexportData(source, GetExportName(exportSpecifier.Exported), GetExportName(exportSpecifier.Local)));
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
                                variableDeclarationVisitor.VisitVariableDeclaratorId(declarations[i]);
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
                ModuleResource source = _bundler.ResolveImport(importDeclaration.Source.StringValue, _module.Resource);

                ref readonly NodeList<ImportDeclarationSpecifier> specifiers = ref importDeclaration.Specifiers;
                for (var i = 0; i < specifiers.Count; i++)
                    switch (specifiers[i])
                    {
                        //  import localName from 'src/my_lib';
                        case ImportDefaultSpecifier importDefaultSpecifier:
                            _module.Imports[importDefaultSpecifier.Local.Name] = new NamedImportData(source, importDefaultSpecifier.Local.Name, ExportName.Default);
                            break;
                        // import * as my_lib from 'src/my_lib';
                        case ImportNamespaceSpecifier importNamespaceSpecifier:
                            _module.Imports[importNamespaceSpecifier.Local.Name] = new NamespaceImportData(source, importNamespaceSpecifier.Local.Name);
                            break;
                        // import { name1 } from 'src/my_lib';
                        // import { default as foo } from 'src/my_lib';
                        case ImportSpecifier importSpecifier:
                            _module.Imports[importSpecifier.Local.Name] = new NamedImportData(source, importSpecifier.Local.Name, GetExportName(importSpecifier.Imported));
                            break;
                    }

                if (!_module.ModuleRefs.ContainsKey(source))
                    _module.ModuleRefs[source] = GetModuleRef(_moduleIndex++);
            }

            protected override object VisitAwaitExpression(AwaitExpression awaitExpression)
            {
                if (CurrentVariableScope.FunctionScope is VariableScope.TopLevelBlock)
                {
                    _bundler._synthesizeAsyncLoader = true;
                }

                Visit(awaitExpression.Argument);

                return awaitExpression;
            }

            protected override object VisitExportAllDeclaration(ExportAllDeclaration exportAllDeclaration)
            {
                base.VisitExportAllDeclaration(exportAllDeclaration);

                if (exportAllDeclaration.Attributes.Count > 0)
                {
                    _bundler._logger.IgnoredImportAttributesWarning(_module.Resource.Url.ToString(), exportAllDeclaration.Location.Start);
                }

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

                if (exportNamedDeclaration.Attributes.Count > 0)
                {
                    _bundler._logger.IgnoredImportAttributesWarning(_module.Resource.Url.ToString(), exportNamedDeclaration.Location.Start);
                }

                ExtractExports(exportNamedDeclaration);

                return exportNamedDeclaration;
            }

            protected override object VisitImportDeclaration(ImportDeclaration importDeclaration)
            {
                base.VisitImportDeclaration(importDeclaration);

                if (importDeclaration.Attributes.Count > 0)
                {
                    _bundler._logger.IgnoredImportAttributesWarning(_module.Resource.Url.ToString(), importDeclaration.Location.Start);
                }

                ExtractImports(importDeclaration);

                return importDeclaration;
            }

            protected override object VisitImportExpression(ImportExpression importExpression)
            {
                if (IsRewritableDynamicImport(importExpression, out Literal sourceLiteral))
                {
                    if (importExpression.Options != null)
                    {
                        _bundler._logger.IgnoredImportAttributesWarning(_module.Resource.Url.ToString(), importExpression.Location.Start);
                    }

                    ModuleResource source = _bundler.ResolveImport(sourceLiteral.StringValue, _module.Resource);

                    if (!_module.ModuleRefs.ContainsKey(source))
                        _module.ModuleRefs[source] = GetModuleRef(_moduleIndex++);
                }
                else
                    _bundler._logger.NonRewritableDynamicImportWarning(_module.Resource.Url.ToString(), importExpression.Location.Start);

                return importExpression;
            }

            protected override object VisitMetaProperty(MetaProperty metaProperty)
            {
                if (IsImportMeta(metaProperty))
                    _module.UsesImportMeta = true;

                return metaProperty;
            }
        }

        private void AnalyzeDeclarations(ModuleData module) => new VariableDeclarationAnalyzer(this, module).Analyze();
    }
}
