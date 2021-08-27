using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Esprima;
using Esprima.Ast;
using Karambolo.AspNetCore.Bundling.EcmaScript.Internal.Helpers;
using Karambolo.AspNetCore.Bundling.Internal;
using Karambolo.AspNetCore.Bundling.Internal.Helpers;
using Microsoft.Extensions.Primitives;

namespace Karambolo.AspNetCore.Bundling.EcmaScript.Internal
{
    using Range = Esprima.Ast.Range;

    internal partial class ModuleBundler
    {
        private delegate void SubstitutionAdjuster(Identifier identifier, ref string value);

        private sealed class SubstitutionCollector : ImprovedAstVisitor
        {
            private readonly ModuleBundler _bundler;
            private readonly ModuleData _module;

            // ranges must not overlap!
            private readonly SortedDictionary<Range, StringSegment> _substitutions;

            private VariableScope _currentVariableScope;

            public SubstitutionCollector(ModuleBundler bundler, ModuleData module, SortedDictionary<Range, StringSegment> substitutions)
            {
                _bundler = bundler;
                _module = module;
                _substitutions = substitutions;
            }

            public void Collect()
            {
                Visit(_module.Ast);
            }

            public override void Visit(Node node)
            {
                VariableScope previousVariableScope = _currentVariableScope;
                if (_module.VariableScopes.TryGetValue(node, out VariableScope variableScope))
                    _currentVariableScope = variableScope;

                base.Visit(node);

                _currentVariableScope = previousVariableScope;
            }

            private Scanner CreateScannerFor(Node node) => new Scanner(_module.Content, _module.ParserOptions)
            {
                Index = node.Range.Start,
                LineNumber = node.Location.Start.Line,
                LineStart = node.Range.Start - node.Location.Start.Column
            };

            private VariableDeclarationVisitor<SubstitutionCollector> CreateVariableDeclarationVisitor() =>
                new VariableDeclarationVisitor<SubstitutionCollector>(this, visitRewritableExpression: (sc, expression) => sc.Visit(expression));

            private void AddSubstitutionIfImported(Identifier identifier, SubstitutionAdjuster adjust)
            {
                if (!_module.Imports.TryGetValue(identifier.Name, out ImportData import) ||
                    !(_currentVariableScope.FindIdentifier(identifier.Name) is VariableScope.GlobalBlock))
                    return;

                string value;
                switch (import)
                {
                    case NamedImportData namedImport:
                        value = GetModuleVariableRef(_module.ModuleRefs[import.Source], namedImport.ImportName);
                        break;
                    case NamespaceImportData _:
                        value = _module.ModuleRefs[import.Source];
                        break;
                    default:
                        return;
                }

                adjust(identifier, ref value);
                _substitutions.Add(identifier.Range, value);
            }

            protected override void VisitBreakStatement(BreakStatement breakStatement)
            {
                // Label identifier is not subject to rewriting, thus, skipped.
            }

            protected override void VisitCatchClause(CatchClause catchClause)
            {
                // Catch clause error parameter identifier(s) are not subject to rewriting, thus, skipped.
                CreateVariableDeclarationVisitor().VisitParam(catchClause);

                Visit(catchClause.Body);
            }

            protected override void VisitClassDeclaration(ClassDeclaration classDeclaration)
            {
                // Class name identifier is not subject to rewriting, thus, skipped.

                if (classDeclaration.SuperClass != null)
                    Visit(classDeclaration.SuperClass);

                Visit(classDeclaration.Body);
            }

            protected override void VisitClassExpression(ClassExpression classExpression)
            {
                // Class name identifier is not subject to rewriting, thus, skipped.

                if (classExpression.SuperClass != null)
                    Visit(classExpression.SuperClass);

                Visit(classExpression.Body);
            }

            protected override void VisitContinueStatement(ContinueStatement continueStatement)
            {
                // Label identifier is not subject to rewriting, thus, skipped.
            }

            protected override void VisitExportAllDeclaration(ExportAllDeclaration exportAllDeclaration)
            {
                _substitutions.Add(exportAllDeclaration.Range, StringSegment.Empty);

                base.VisitExportAllDeclaration(exportAllDeclaration);
            }

            protected override void VisitExportDefaultDeclaration(ExportDefaultDeclaration exportDefaultDeclaration)
            {
                switch (exportDefaultDeclaration.Declaration)
                {
                    case FunctionDeclaration functionDeclaration when functionDeclaration.Id != null:
                    case ClassDeclaration classDeclaration when classDeclaration.Id != null:
                        _substitutions.Add(new Range(exportDefaultDeclaration.Range.Start, FindActualDeclarationStart(exportDefaultDeclaration)), StringSegment.Empty);
                        break;
                    default:
                        _substitutions.Add(new Range(exportDefaultDeclaration.Range.Start, FindActualDeclarationStart(exportDefaultDeclaration)), string.Concat("var ", DefaultExportId, " = "));
                        break;
                }

                base.VisitExportDefaultDeclaration(exportDefaultDeclaration);

                // exportDefaultDeclaration.Declaration.Range doesn't include preceding comments or parentheses,
                // so we need to do some gymnastics to determine the actual start of the declaration/expression.
                int FindActualDeclarationStart(ExportDefaultDeclaration declaration)
                {
                    Scanner scanner = CreateScannerFor(declaration);

                    scanner.Lex(); // skip export keyword
                    scanner.ScanComments(); // skip possible comments/whitespace
                    scanner.Lex(); // skip default keyword
                    scanner.ScanComments(); // skip possible comments/whitespace

                    return scanner.Index;
                }
            }

            protected override void VisitExportNamedDeclaration(ExportNamedDeclaration exportNamedDeclaration)
            {
                if (exportNamedDeclaration.Declaration == null)
                    _substitutions.Add(exportNamedDeclaration.Range, StringSegment.Empty);
                else
                    _substitutions.Add(new Range(exportNamedDeclaration.Range.Start, exportNamedDeclaration.Declaration.Range.Start), StringSegment.Empty);

                base.VisitExportNamedDeclaration(exportNamedDeclaration);
            }

            protected override void VisitExportSpecifier(ExportSpecifier exportSpecifier)
            {
                // Specifier identifiers are not subject to rewriting, thus, skipped.
            }

            protected override void VisitFunctionExpression(IFunction function)
            {
                // Class name identifier is not subject to rewriting, thus, skipped.

                // Function parameter identifier(s) are not subject to rewriting, thus, skipped.
                CreateVariableDeclarationVisitor().VisitParams(function);

                Visit(function.Body);
            }

            protected override void VisitIdentifier(Identifier identifier)
            {
                AddSubstitutionIfImported(identifier, delegate { });
            }

            protected override void VisitImportDeclaration(ImportDeclaration importDeclaration)
            {
                _substitutions.Add(importDeclaration.Range, StringSegment.Empty);

                base.VisitImportDeclaration(importDeclaration);
            }

            protected override void VisitImportDefaultSpecifier(ImportDefaultSpecifier importDefaultSpecifier)
            {
                // Specifier identifiers are not subject to rewriting, thus, skipped.
            }

            protected override void VisitImportNamespaceSpecifier(ImportNamespaceSpecifier importNamespaceSpecifier)
            {
                // Specifier identifiers are not subject to rewriting, thus, skipped.
            }

            protected override void VisitImportSpecifier(ImportSpecifier importSpecifier)
            {
                // Specifier identifiers are not subject to rewriting, thus, skipped.
            }

            protected override void VisitLabeledStatement(LabeledStatement labeledStatement)
            {
                // Label identifier is not subject to rewriting, thus, skipped.

                Visit(labeledStatement.Body);
            }

            protected override void VisitMemberExpression(MemberExpression memberExpression)
            {
                Visit(memberExpression.Object);

                // Property name identifier is not subject to rewriting, thus, skipped.
                // Computed properties (array-like access) needs to be visited though.
                if (memberExpression.Computed)
                    Visit(memberExpression.Property);
            }

            protected override void VisitMetaProperty(MetaProperty metaProperty)
            {
                _substitutions.Add(metaProperty.Range, ImportMetaId);
            }

            protected override void VisitMethodDefinition(MethodDefinition methodDefinitions)
            {
                // Method name identifier is not subject to rewriting, thus, skipped.
                // Computed keys needs to be visited though.
                if (methodDefinitions.Computed)
                    Visit(methodDefinitions.Key);

                Visit(methodDefinitions.Value);
            }

            protected override void VisitProperty(Property property)
            {
                // Shorthand properties need special care.
                if (property.Shorthand)
                {
                    var identifier = (Identifier)property.Value;
                    AddSubstitutionIfImported(identifier, delegate (Identifier id, ref string value) { value = id.Name + ": " + value; });
                    return;
                }

                // Property name identifier is not subject to rewriting, thus, skipped.
                // Computed keys needs to be visited though.
                if (property.Computed)
                    Visit(property.Key);

                Visit(property.Value);
            }

            protected override void VisitUnknownNode(Node node)
            {
                Range range = node.Range;
                throw _bundler._logger.RewritingModuleFailed(_module.Resource.Url.ToString(), node.Location.Start,
                    $"'{_module.Content.Substring(range.Start, range.End - range.Start)}' is not supported currently.");
            }

            protected override void VisitVariableDeclaration(VariableDeclaration variableDeclaration)
            {
                VariableDeclarationVisitor<SubstitutionCollector> variableDeclarationVisitor = CreateVariableDeclarationVisitor();

                for (int i = 0, n = variableDeclaration.Declarations.Count; i < n; i++)
                {
                    VariableDeclarator variableDeclarator = variableDeclaration.Declarations[i];

                    // Variable identifier(s) are not subject to rewriting, thus, skipped.
                    variableDeclarationVisitor.VisitId(variableDeclarator);

                    if (variableDeclarator.Init != null)
                        Visit(variableDeclarator.Init);
                }
            }

            protected override void VisitWithStatement(WithStatement withStatement)
            {
                // Modules are always in strict mode, which doesn't allow with statements.
                throw _bundler._logger.RewritingModuleFailed(_module.Resource.Url.ToString(), withStatement.Location.Start,
                    "With statements are not supported in ES6 modules.");
            }
        }

        private sealed class DescendingRangeComparer : IComparer<Range>
        {
            public static readonly DescendingRangeComparer Instance = new DescendingRangeComparer();

            private DescendingRangeComparer() { }

            public int Compare(Range x, Range y)
            {
                return y.Start.CompareTo(x.Start);
            }
        }

        private sealed class RewriteModuleLocals
        {
            public HashSet<ModuleData> VisitedModules { get; } = new HashSet<ModuleData>();
            public Dictionary<string, ExportData> Exports { get; } = new Dictionary<string, ExportData>();
            public SortedDictionary<Range, StringSegment> Substitutions { get; } = new SortedDictionary<Range, StringSegment>(DescendingRangeComparer.Instance);
            public StringBuilder StringBuilder { get; } = new StringBuilder();

            public void Reset()
            {
                VisitedModules.Clear();
                Exports.Clear();
                Substitutions.Clear();
                StringBuilder.Clear();
                if (StringBuilder.Capacity > 4096)
                    StringBuilder.Capacity = 4096;
            }
        }

        private const string RequireId = "__es$require";
        private const string RequireDefineId = "d";
        private const string ExportsId = "__es$exports";
        private const string ModuleIdPrefix = "__es$module_";
        private const string DefaultExportId = "__es$default";
        private const string ImportMetaId = "__es$importMeta";

        private static StringBuilder ReplaceRange(StringBuilder sb, string content, int offset, in Range range, in StringSegment segment)
        {
            var endIndex = range.End;

            if (segment.Length == 0 && endIndex > range.Start && content.Length > endIndex)
                switch (content[endIndex])
                {
                    case '\r':
                        endIndex++;
                        if (content.Length > endIndex && content[endIndex] == '\n')
                            endIndex++;
                        break;
                    case '\n':
                        endIndex++;
                        break;
                }

            return sb.Substitute(offset + range.Start, endIndex - range.Start, segment);
        }

        private static string GetModuleRef(int uniqueId)
        {
            return ModuleIdPrefix + uniqueId;
        }

        private static string GetModuleVariableRef(string moduleRef, string localName)
        {
            return moduleRef + "." + localName;
        }

        private void AppendPolyfills(StringBuilder sb, ModuleData module)
        {
            if (_developmentMode)
            {
                sb.Append(_br);

                if (_developmentMode)
                    sb.Append("/* Polyfills */").Append(_br);
            }

            if (module.UsesImportMeta)
            {
                sb.Append("var ").Append(ImportMetaId).Append(" = ").Append("{ }").Append(';').Append(_br);

                sb.Append(RequireId).Append('.').Append(RequireDefineId).Append('(')
                    .Append(ImportMetaId).Append(", ")
                    .Append('"').Append("url").Append('"').Append(", ")
                    .Append("function() { return ").Append('"').Append(module.Resource.SecureUrl).Append('"').Append("; }").Append(')').Append(';').Append(_br);
            }
        }

        private void AppendImports(StringBuilder sb, ModuleData module)
        {
            if (_developmentMode)
            {
                sb.Append(_br);

                if (_developmentMode)
                    sb.Append("/* Imports */").Append(_br);
            }

            foreach ((IModuleResource resource, string moduleRef) in module.ModuleRefs)
                sb.Append("var ").Append(moduleRef).Append(" = ").Append(RequireId).Append('(')
                    .Append('"').Append(resource.Id).Append('"').Append(')').Append(';').Append(_br);
        }

        private StringBuilder AppendNamedExportDefinition(StringBuilder sb, string exportName, string localExpression)
        {
            return sb.Append(RequireId).Append('.').Append(RequireDefineId).Append('(')
                .Append(ExportsId).Append(", ")
                .Append('"').Append(exportName).Append('"').Append(", ")
                .Append("function() { return ").Append(localExpression).Append("; }").Append(')');
        }

        private void AppendExports(StringBuilder sb, ModuleData module, Dictionary<string, ExportData> exports)
        {
            if (_developmentMode)
            {
                sb.Append(_br);

                if (_developmentMode)
                    sb.Append("/* Exports */").Append(_br);
            }

            foreach (ExportData export in exports.Values)
            {
                switch (export)
                {
                    case ReexportData reexport:
                        AppendNamedExportDefinition(sb, reexport.ExportName, GetModuleVariableRef(module.ModuleRefs[reexport.Source], reexport.LocalName));
                        break;
                    case NamedExportData namedExport:
                        AppendNamedExportDefinition(sb, namedExport.ExportName, namedExport.LocalName);
                        break;
                }
                sb.Append(';').Append(_br);
            }
        }

        private void ExpandExports(Dictionary<string, ExportData> exports, HashSet<ModuleData> visitedModules, ModuleData module,
            ModuleData rootModule, IModuleResource exportAllSource = null)
        {
            // TODO: wildcard re-exports may cause redeclarations ()

            for (int i = 0, n = module.ExportsRaw.Count; i < n; i++)
            {
                ExportData export = module.ExportsRaw[i];

                // wildcard re-exports (export * from '...';)
                if (export is ExportAllData exportAll)
                {
                    ModuleData reexportedModule = Modules[exportAll.Source];

                    // detecting circular references
                    if (visitedModules.Add(reexportedModule))
                        ExpandExports(exports, visitedModules, reexportedModule, rootModule, exportAllSource ?? exportAll.Source);
                }
                // normal exports of the root module
                else if (exportAllSource == null)
                    exports[export.ExportName] = export;
                // normal exports of other modules when expanding a wildcard re-export
                // (except for default exports, which aren't visible in this case)
                else if (export.ExportName != DefaultExportName)
                    exports[export.ExportName] = new ReexportData(exportAllSource, export.ExportName, export.ExportName);
            }
        }

        private RewriteModuleLocals RewriteModule(ModuleData module, ParallelLoopState loopState, RewriteModuleLocals locals)
        {
            locals.Reset();

            HashSet<ModuleData> visitedModules = locals.VisitedModules;
            Dictionary<string, ExportData> exports = locals.Exports;
            SortedDictionary<Range, StringSegment> substitutions = locals.Substitutions;
            StringBuilder sb = locals.StringBuilder;

            sb.Append("'use strict';").Append(_br);

            // polyfills

            if (module.UsesImportMeta)
                AppendPolyfills(sb, module);

            // require modules

            if (module.ModuleRefs.Count > 0)
                AppendImports(sb, module);

            // define exports

            visitedModules.Add(module);
            ExpandExports(exports, visitedModules, module, module);

            if (exports.Count > 0)
                AppendExports(sb, module, exports);

            // content

            if (_developmentMode)
                sb.Append(_br);

            new SubstitutionCollector(this, module, substitutions).Collect();

            int offset = sb.Length;

            sb.Append(module.Content);

            foreach ((Range range, StringSegment segment) in substitutions)
                ReplaceRange(sb, module.Content, offset, range, in segment);

            module.Content = sb.ToString();

            return locals;
        }

        private ModuleBundlingResult BuildResult(ModuleData[] rootModules)
        {
            var sb = new StringBuilder(
$@"(function (modules) {{
    var moduleCache = {{}};

    function {RequireId}(moduleId) {{
        var module = moduleCache[moduleId];
        if (!module) {{
            moduleCache[moduleId] = module = {{
                id: moduleId,
                exports: {{}}
            }};
            modules[moduleId].call(module.exports, {RequireId}, module.exports);
        }}
        return module.exports;
    }}

    {RequireId}.d = function (exports, name, getter) {{
        if (!Object.prototype.hasOwnProperty.call(exports, name))
            Object.defineProperty(exports, name, {{ enumerable: true, get: getter, set: function() {{ throw new TypeError('Assignment to constant variable.'); }} }});
    }};

");

            for (int i = 0, n = rootModules.Length; i < n; i++)
            {
                ModuleData module = rootModules[i];
                sb.Append(' ', 4).Append($"{RequireId}(\"{module.Resource.Id}\");").Append(_br);
            }

            sb.Append("})({");

            var isFirstAppend = true;

            foreach (ModuleData module in Modules.Values)
            {
                if (isFirstAppend)
                    isFirstAppend = false;
                else
                    sb.Append(',');

                sb.Append(_br).Append(' ', 4).Append($"\"{module.Resource.Id}\": function ({RequireId}, {ExportsId}) {{").Append(_br);

                if (_developmentMode)
                {
                    var index = sb.Length;
                    sb.Append('/').Append('*', 3)
                        .Append($" MODULE: {module.Resource.Url} ")
                        .Append('*', Math.Max(78 - (sb.Length - index), 0)).Append('*').Append('/')
                        .Append(_br);
                }

                sb.Append(module.Content).Append(_br);

                if (_developmentMode)
                    sb.Append('/').Append('*', 78).Append('/').Append(_br);

                sb.Append(' ', 4).Append('}');
            }

            sb.Append(_br).Append("});").Append(_br);

            var imports = new HashSet<AbstractionFile>();

            foreach ((FileModuleResource fileResource, (bool isRoot, _)) in Files)
                if (!isRoot)
                    imports.Add(new AbstractionFile(fileResource.FileProvider, fileResource.FilePath, fileResource.CaseSensitiveFilePaths));

            return new ModuleBundlingResult(sb.ToString(), imports);
        }
    }
}
