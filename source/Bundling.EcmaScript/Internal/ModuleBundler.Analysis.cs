using Esprima;
using Esprima.Ast;
using Karambolo.AspNetCore.Bundling.EcmaScript.Internal.Helpers;

namespace Karambolo.AspNetCore.Bundling.EcmaScript.Internal
{
    internal partial class ModuleBundler
    {
        private sealed class VariableDeclarationAnalyzer : VariableScopeBuilder
        {
            private readonly ModuleBundler _bundler;
            private readonly ModuleData _module;

            private int _moduleIndex;

            public VariableDeclarationAnalyzer(ModuleBundler bundler, ModuleData module) : base(module.VariableScopes)
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

            private void ExtractExports(ExportAllDeclaration exportAllDeclaration)
            {
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
                        for (int i = 0, n = exportNamedDeclaration.Specifiers.Count; i < n; i++)
                        {
                            ExportSpecifier exportSpecifier = exportNamedDeclaration.Specifiers[i];
                            _module.ExportsRaw.Add(new NamedExportData(exportSpecifier.Exported.Name, exportSpecifier.Local.Name));
                        }
                    }
                    // export { default as defaultAlias, a as alias, b } from 'bar.js';
                    else
                    {
                        IModuleResource source = ResolveImportSource(exportNamedDeclaration.Source.StringValue);

                        for (int i = 0, n = exportNamedDeclaration.Specifiers.Count; i < n; i++)
                        {
                            ExportSpecifier exportSpecifier = exportNamedDeclaration.Specifiers[i];

                            _module.ExportsRaw.Add(new ReexportData(source, exportSpecifier.Exported.Name, exportSpecifier.Local.Name));
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
                IModuleResource source = ResolveImportSource(importDeclaration.Source.StringValue);

                for (int i = 0, n = importDeclaration.Specifiers.Count; i < n; i++)
                    switch (importDeclaration.Specifiers[i])
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
                            _module.Imports[importSpecifier.Local.Name] = new NamedImportData(source, importSpecifier.Local.Name, importSpecifier.Imported.Name);
                            break;
                    }

                if (!_module.ModuleRefs.ContainsKey(source))
                    _module.ModuleRefs[source] = GetModuleRef(_moduleIndex++);
            }

            internal static bool IsDynamicImportCall(CallExpression callExpression, out Literal sourceLiteral)
            {
                if (callExpression.Callee is Import)
                {
                    sourceLiteral = 
                        callExpression.Arguments.Count == 1 &&
                            callExpression.Arguments[0] is Literal literal &&
                            literal.TokenType == TokenType.StringLiteral ? 
                        literal :
                        null;

                    return true;
                }

                sourceLiteral = default;
                return false;
            }

            protected override void VisitCallExpression(CallExpression callExpression)
            {
                if (IsDynamicImportCall(callExpression, out Literal sourceLiteral))
                {
                    if (sourceLiteral != null)
                    {
                        IModuleResource source = ResolveImportSource(sourceLiteral.StringValue);

                        if (!_module.ModuleRefs.ContainsKey(source))
                            _module.ModuleRefs[source] = GetModuleRef(_moduleIndex++);
                    }
                    else
                        _bundler._logger.NonRewritableDynamicImportWarning(_module.Resource.Url.ToString(), callExpression.Location.Start);
                }
                else
                    base.VisitCallExpression(callExpression);
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
