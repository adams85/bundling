using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Acornima;
using Acornima.Ast;
using Karambolo.AspNetCore.Bundling.EcmaScript.Internal.Helpers;
using Karambolo.AspNetCore.Bundling.Internal;
using Karambolo.AspNetCore.Bundling.Internal.Helpers;
using Microsoft.Extensions.Primitives;

namespace Karambolo.AspNetCore.Bundling.EcmaScript.Internal
{
    using ExportDictionary = Dictionary<string, (ModuleBundler.ExportData Export, bool IsExportedViaWildcard)>;
    using Range = Acornima.Range;

    internal partial class ModuleBundler
    {
        private delegate void SubstitutionAdjuster(Identifier identifier, ref string value);

        private sealed class SubstitutionCollector : AstVisitor
        {
            private readonly ModuleBundler _bundler;
            private readonly ModuleData _module;

            // ranges must not overlap!
            private readonly SortedDictionary<Range, StringSegment> _substitutions;

            private VariableScope _currentVariableScope;
            private Tokenizer _tokenizer;

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

            public override object Visit(Node node)
            {
                VariableScope previousVariableScope = _currentVariableScope;
                if (node.UserData is VariableScope variableScope)
                    _currentVariableScope = variableScope;

                var result = base.Visit(node);

                _currentVariableScope = previousVariableScope;

                return result;
            }

            private void SetTokenizerTo(Node node)
            {
                var start = node.Range.Start;
                if (_tokenizer == null)
                    _tokenizer = new Tokenizer(_module.Content, start, _module.Content.Length - start, SourceType.Module, sourceFile: null, _bundler._parserOptions.GetTokenizerOptions());
                else
                    _tokenizer.Reset(_module.Content, start, SourceType.Module);
            }

            private VariableDeclarationVisitor<SubstitutionCollector> CreateVariableDeclarationVisitor() =>
                new VariableDeclarationVisitor<SubstitutionCollector>(this, visitRewritableExpression: (sc, expression) => sc.Visit(expression));

            private void AddSubstitutionIfImported(Identifier identifier, SubstitutionAdjuster adjust)
            {
                if (!_module.Imports.TryGetValue(identifier.Name, out ImportData import) ||
                    !(_currentVariableScope.FindIdentifier(identifier.Name) is VariableScope.TopLevelBlock))
                    return;

                string value;
                switch (import)
                {
                    case NamedImportData namedImport:
                        value = GetImportVariableRef(_module.ModuleRefs[import.Source], namedImport.ImportName);
                        break;
                    case NamespaceImportData:
                        value = _module.ModuleRefs[import.Source];
                        break;
                    default:
                        return;
                }

                adjust(identifier, ref value);
                _substitutions.Add(identifier.Range, value);
            }

            protected override object VisitAccessorProperty(AccessorProperty node)
            {
                ref readonly NodeList<Decorator> decorators = ref node.Decorators;
                for (var i = 0; i < decorators.Count; i++)
                {
                    Visit(decorators[i]);
                }

                VisitPropertyCore(node);

                if (node.Value is not null)
                {
                    Visit(node.Value);
                }

                return node;
            }

            protected override object VisitArrowFunctionExpression(ArrowFunctionExpression node)
            {
                VisitFunctionCore(node);

                return node;
            }

            protected override object VisitBreakStatement(BreakStatement node)
            {
                // Label identifier is not subject to rewriting, thus, skipped.

                return node;
            }

            protected override object VisitCatchClause(CatchClause node)
            {
                // Catch clause error parameter identifier(s) are not subject to rewriting, thus, skipped.
                CreateVariableDeclarationVisitor().VisitCatchClauseParam(node);

                Visit(node.Body);

                return node;
            }

            private void VisitClassCore(IClass node)
            {
                ref readonly NodeList<Decorator> decorators = ref node.Decorators;
                for (var i = 0; i < decorators.Count; i++)
                {
                    Visit(decorators[i]);
                }

                // Class name identifier is not subject to rewriting, thus, skipped.

                if (node.SuperClass != null)
                    Visit(node.SuperClass);

                Visit(node.Body);
            }

            protected override object VisitClassDeclaration(ClassDeclaration node)
            {
                VisitClassCore(node);

                return node;
            }

            protected override object VisitClassExpression(ClassExpression node)
            {
                VisitClassCore(node);

                return node;
            }

            protected override object VisitContinueStatement(ContinueStatement node)
            {
                // Label identifier is not subject to rewriting, thus, skipped.

                return node;
            }

            protected override object VisitExportAllDeclaration(ExportAllDeclaration node)
            {
                _substitutions.Add(node.Range, StringSegment.Empty);

                return node;
            }

            protected override object VisitExportDefaultDeclaration(ExportDefaultDeclaration node)
            {
                switch (node.Declaration)
                {
                    case FunctionDeclaration functionDeclaration when functionDeclaration.Id != null:
                    case ClassDeclaration classDeclaration when classDeclaration.Id != null:
                        _substitutions.Add(Range.From(node.Range.Start, FindActualDeclarationStart(node)), StringSegment.Empty);
                        break;
                    default:
                        _substitutions.Add(Range.From(node.Range.Start, FindActualDeclarationStart(node)), string.Concat("var ", DefaultExportId, " = "));
                        break;
                }

                Visit(node.Declaration);

                return node;

                // exportDefaultDeclaration.Declaration.Range doesn't include preceding comments or parentheses,
                // so we need to do some gymnastics to determine the actual start of the declaration/expression.
                int FindActualDeclarationStart(ExportDefaultDeclaration declaration)
                {
                    SetTokenizerTo(declaration);
                    _tokenizer.Next(); // skip export keyword
                    _tokenizer.Next(); // skip default keyword
                    return _tokenizer.GetToken().Start;
                }
            }

            protected override object VisitExportNamedDeclaration(ExportNamedDeclaration node)
            {
                if (node.Declaration != null)
                {
                    _substitutions.Add(Range.From(node.Range.Start, node.Declaration.Range.Start), StringSegment.Empty);

                    Visit(node.Declaration);
                }
                else
                    _substitutions.Add(node.Range, StringSegment.Empty);

                return node;
            }

            protected override object VisitExportSpecifier(ExportSpecifier node)
            {
                // Specifier identifiers are not subject to rewriting, thus, skipped.

                return node;
            }

            private void VisitFunctionCore(IFunction node)
            {
                // Function name identifier is not subject to rewriting, thus, skipped.

                // Function parameter identifier(s) are not subject to rewriting, thus, skipped.
                CreateVariableDeclarationVisitor().VisitFunctionParams(node);

                Visit(node.Body);
            }

            protected override object VisitFunctionDeclaration(FunctionDeclaration node)
            {
                VisitFunctionCore(node);

                return node;
            }

            protected override object VisitFunctionExpression(FunctionExpression node)
            {
                VisitFunctionCore(node);

                return node;
            }

            protected override object VisitIdentifier(Identifier node)
            {
                AddSubstitutionIfImported(node, delegate { });

                return node;
            }

            protected override object VisitImportDeclaration(ImportDeclaration node)
            {
                _substitutions.Add(node.Range, StringSegment.Empty);

                return node;
            }

            protected override object VisitImportDefaultSpecifier(ImportDefaultSpecifier node)
            {
                // Specifier identifiers are not subject to rewriting, thus, skipped.

                return node;
            }

            protected override object VisitImportExpression(ImportExpression node)
            {
                if (IsRewritableDynamicImport(node, out StringLiteral sourceLiteral))
                {
                    ModuleResource source = _bundler.ResolveImport(sourceLiteral.Value, _module.Resource);
                    var moduleRef = _module.ModuleRefs[source];
                    _substitutions.Add(node.Range, $"Promise.resolve({moduleRef})");
                }
                else
                    return base.VisitImportExpression(node);

                return node;
            }

            protected override object VisitImportNamespaceSpecifier(ImportNamespaceSpecifier node)
            {
                // Specifier identifiers are not subject to rewriting, thus, skipped.

                return node;
            }

            protected override object VisitImportSpecifier(ImportSpecifier node)
            {
                // Specifier identifiers are not subject to rewriting, thus, skipped.

                return node;
            }

            protected override object VisitLabeledStatement(LabeledStatement node)
            {
                // Label identifier is not subject to rewriting, thus, skipped.

                Visit(node.Body);

                return node;
            }

            protected override object VisitMemberExpression(MemberExpression node)
            {
                Visit(node.Object);

                // Property name identifier is not subject to rewriting, thus, skipped.
                // Computed properties (array-like access) needs to be visited though.
                if (node.Computed)
                    Visit(node.Property);

                return node;
            }

            protected override object VisitMetaProperty(MetaProperty node)
            {
                if (IsImportMeta(node))
                    _substitutions.Add(node.Range, ImportMetaId);

                return node;
            }

            protected override object VisitMethodDefinition(MethodDefinition node)
            {
                ref readonly NodeList<Decorator> decorators = ref node.Decorators;
                for (var i = 0; i < decorators.Count; i++)
                {
                    Visit(decorators[i]);
                }

                VisitPropertyCore(node);

                Visit(node.Value);

                return node;
            }

            private void VisitPropertyCore(IProperty node)
            {
                // Property name identifier is not subject to rewriting, thus, skipped.
                // Computed keys needs to be visited though.
                if (node.Computed)
                    Visit(node.Key);
            }

            protected override object VisitProperty(Property node)
            {
                // Shorthand properties need special care.
                if (node.Shorthand)
                {
                    var identifier = (Identifier)node.Value;
                    AddSubstitutionIfImported(identifier, delegate (Identifier id, ref string value) { value = id.Name + ": " + value; });
                }
                else
                {
                    VisitPropertyCore(node);

                    Visit(node.Value);
                }

                return node;
            }

            protected override object VisitPropertyDefinition(PropertyDefinition node)
            {
                ref readonly NodeList<Decorator> decorators = ref node.Decorators;
                for (var i = 0; i < decorators.Count; i++)
                {
                    Visit(decorators[i]);
                }

                VisitPropertyCore(node);

                if (node.Value != null)
                {
                    Visit(node.Value);
                }

                return node;
            }

            protected override object VisitVariableDeclaration(VariableDeclaration node)
            {
                VariableDeclarationVisitor<SubstitutionCollector> variableDeclarationVisitor = CreateVariableDeclarationVisitor();

                ref readonly NodeList<VariableDeclarator> declarations = ref node.Declarations;
                for (var i = 0; i < declarations.Count; i++)
                {
                    VariableDeclarator variableDeclarator = declarations[i];

                    // Variable identifier(s) are not subject to rewriting, thus, skipped.
                    variableDeclarationVisitor.VisitVariableDeclaratorId(variableDeclarator);

                    if (variableDeclarator.Init != null)
                        Visit(variableDeclarator.Init);
                }

                return variableDeclarationVisitor;
            }

            // Modules are always in strict mode, which doesn't allow with statements.
            //protected override object VisitWithStatement(WithStatement node)
            //{
            //    return base.VisitWithStatement(node);
            //}
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
            public ExportDictionary Exports { get; } = new ExportDictionary();
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

        private const string ModuleIdPrefix = "__es$module_";
        private const string RequireId = "__es$require";
        private const string FinalizeId = "__es$finalize";
        private const string DefineId = "__es$define";
        private const string DefaultExportId = "__es$default";
        private const string ImportMetaId = "__es$importMeta";
        private const int IndentSize = 4;

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

        private static string GetImportVariableRef(string moduleRef, ExportName importName)
        {
            return
                !importName.IsLiteral ?
                moduleRef + "." + importName.Value :
                moduleRef + "[" + importName.RawValue + "]";
        }

        private void AppendShims(StringBuilder sb, ModuleData module)
        {
            if (_developmentMode)
            {
                sb.Append(_br);

                if (_developmentMode)
                    sb.Append("/* Shims */").Append(_br);
            }

            if (module.UsesImportMeta)
            {
                sb.Append("var ").Append(ImportMetaId).Append(" = ").Append("{ }").Append(';').Append(_br);

                var escapedResourceUrl = HttpUtility.JavaScriptStringEncode(module.Resource.DesensitizedUrl.ToStringEscaped());
                sb.Append(DefineId).Append('(')
                    .Append(ImportMetaId).Append(", {").Append(_br)
                    .Append(' ', IndentSize).Append("url").Append(": ")
                    .Append("function() { return ").Append('"').Append(escapedResourceUrl).Append('"').Append("; }")
                    .Append(_br).Append("});").Append(_br);
            }
        }

        private void AppendImports(StringBuilder sb, ModuleData module)
        {
            if (module.ModuleRefs.Count > 0)
            {
                if (_developmentMode)
                {
                    sb.Append(_br);

                    if (_developmentMode)
                        sb.Append("/* Imports */").Append(_br);
                }

                foreach ((ModuleResource resource, string moduleRef) in module.ModuleRefs)
                    sb.Append("var ").Append(moduleRef).Append(" = ").Append(RequireId).Append('(')
                        .Append('"').Append(resource.IdEscaped).Append('"').Append(')').Append(';').Append(_br);
            }
        }

        private StringBuilder AppendNamedExportDefinition(StringBuilder sb, ExportName exportName, string localExpression)
        {
            return sb
                .Append(!exportName.IsLiteral ? exportName.Value : exportName.RawValue).Append(": ")
                .Append("function() { return ").Append(localExpression).Append("; }");
        }

        private void AppendExports(StringBuilder sb, ModuleData module, ExportDictionary exports)
        {
            if (_developmentMode && exports.Count > 0)
            {
                sb.Append(_br);

                if (_developmentMode)
                    sb.Append("/* Exports */").Append(_br);
            }

            if (_synthesizeAsyncLoader)
            {
                sb.Append("await ");
            }
            sb.Append(FinalizeId).Append("(");

            if (exports.Count > 0)
            {
                sb.Append('{');
                var separator = string.Empty;
                foreach ((ExportData export, _) in exports.Values)
                {
                    sb.Append(separator).Append(_br)
                        .Append(' ', IndentSize);

                    switch (export)
                    {
                        case NamedExportData namedExport:
                            AppendNamedExportDefinition(sb, namedExport.ExportName, namedExport.LocalName);
                            break;
                        case ReexportData reexport:
                            AppendNamedExportDefinition(sb, reexport.ExportName, GetImportVariableRef(module.ModuleRefs[reexport.Source], reexport.ImportName));
                            break;
                        case WildcardReexportData wildcardReexport:
                            AppendNamedExportDefinition(sb, wildcardReexport.ExportName, module.ModuleRefs[wildcardReexport.Source]);
                            break;
                    }

                    separator = ",";
                }
                sb.Append(_br).Append('}');
            }

            sb.Append(");").Append(_br);
        }

        private void ExpandExports(ExportDictionary exports, ModuleData module, HashSet<ModuleData> visitedModules)
        {
            // * If there are two wildcard exports statements that implicitly re-export the same name, neither one is re-exported.
            //   (See also: https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Statements/export#re-exporting_aggregating)
            // * Exports by identifier and exports by string literal are not distinguished.
            //   (But comparison must be performed with unescaped string literal values!)

            Queue<(WildcardReexportData Reexport, ModuleResource ImportSource)> wildcardReexportQueue = null;
            WildcardReexportData wildcardReexport;

            // 1. Add exports of entry module
            int i, n;
            for (i = 0, n = module.ExportsRaw.Count; i < n; i++)
            {
                ExportData export = module.ExportsRaw[i];

                wildcardReexport = export as WildcardReexportData;
                if (wildcardReexport == null || wildcardReexport.ExportName.HasValue)
                {
                    // Manual re-exports needs special care.
                    if (export is NamedExportData namedExport && module.Imports.TryGetValue(namedExport.LocalName, out ImportData import))
                    {
                        export = import switch
                        {
                            NamedImportData namedImport => new ReexportData(import.Source, export.ExportName, namedImport.ImportName),
                            NamespaceImportData => new WildcardReexportData(import.Source, export.ExportName),
                            _ => throw new InvalidOperationException()
                        };
                    }

                    exports[export.ExportName.Value] = (export, false);
                }
                else
                {
                    if (visitedModules.Count == 0)
                        visitedModules.Add(module);

                    wildcardReexportQueue ??= new Queue<(WildcardReexportData, ModuleResource)>(capacity: 1);
                    wildcardReexportQueue.Enqueue((wildcardReexport, wildcardReexport.Source));
                }
            }

            if (wildcardReexportQueue == null)
                return;

            HashSet<string> exportNamesToRemove = null;

            // 2. Discover exports of wildcard re-exported modules by BFS traversing them
            while (wildcardReexportQueue.Count > 0)
            {
                ModuleResource importSource;
                (wildcardReexport, importSource) = wildcardReexportQueue.Dequeue();

                module = Modules[wildcardReexport.Source];

                for (i = 0, n = module.ExportsRaw.Count; i < n; i++)
                {
                    ExportData export = module.ExportsRaw[i];

                    wildcardReexport = export as WildcardReexportData;
                    if (wildcardReexport == null || wildcardReexport.ExportName.HasValue)
                    {
                        if (!exports.TryGetValue(export.ExportName.Value, out (ExportData Export, bool IsExportedViaWildcard) entry))
                        {
                            export = new ReexportData(importSource, export.ExportName, export.ExportName);
                            exports[export.ExportName.Value] = (export, true);
                        }
                        else if (entry.IsExportedViaWildcard)
                        {
                            exportNamesToRemove ??= new HashSet<string>();
                            exportNamesToRemove.Add(export.ExportName.Value);
                        }
                    }
                    else if (visitedModules.Add(module))
                        wildcardReexportQueue.Enqueue((wildcardReexport, importSource));
                }
            }

            if (exportNamesToRemove == null)
                return;

            // 3. Remove ambigous exports of wildcard re-exported modules  
            foreach (string exportName in exportNamesToRemove)
                exports.Remove(exportName);
        }

        private RewriteModuleLocals RewriteModule(ModuleData module, ParallelLoopState loopState, RewriteModuleLocals locals)
        {
            locals.Reset();

            HashSet<ModuleData> visitedModules = locals.VisitedModules;
            ExportDictionary exports = locals.Exports;
            SortedDictionary<Range, StringSegment> substitutions = locals.Substitutions;
            StringBuilder sb = locals.StringBuilder;

            sb.Append("'use strict';").Append(_br);

            // require imported modules

            AppendImports(sb, module);

            // define module callback

            sb.Append("return ");
            if (_synthesizeAsyncLoader)
            {
                sb.Append("async ");
            }
            sb.Append("function (").Append(FinalizeId);
            if (module.RequiresDefine)
                sb.Append(", ").Append(DefineId);
            sb.Append(") {").Append(_br);

            // shims

            if (module.UsesImportMeta)
                AppendShims(sb, module);

            // define exports

            ExpandExports(exports, module, visitedModules);
            AppendExports(sb, module, exports);

            // module content

            if (_developmentMode)
                sb.Append(_br);

            new SubstitutionCollector(this, module, substitutions).Collect();

            int offset = sb.Length;

            sb.Append(module.Content);

            foreach ((Range range, StringSegment segment) in substitutions)
                ReplaceRange(sb, module.Content, offset, range, in segment);

            sb.Append(_br).Append("};");

            module.Content = sb.ToString();

            return locals;
        }

        private ModuleBundlingResult BuildResult(ModuleData[] rootModules)
        {
            (string async, string await) = _synthesizeAsyncLoader ? ("async ", "await ") : (string.Empty, string.Empty);

            var sb = new StringBuilder();

            sb
                .Append("(function (modules) {").Append(_br)
                .Append("    var moduleCache = {};").Append(_br)
                .Append(_br)
                .Append("    function require(moduleId, imports) {").Append(_br)
                .Append("        var module = moduleCache[moduleId];").Append(_br)
                .Append("        if (!module) {").Append(_br)
                .Append("            moduleCache[moduleId] = module = { id: moduleId, e: {}, i: [] };").Append(_br)
                .Append($"            var {RequireId} = function (id) {{ return require(id, module.i).e; }};").Append(_br)
                .Append("            var importCallback = modules[moduleId](__es$require);").Append(_br)
                .Append($"            var {FinalizeId} = function (properties) {{ define(module.e, properties); return finalize(module.i); }};").Append(_br)
                .Append("            imports.push(importCallback.bind(void 0, __es$finalize, define));").Append(_br)
                .Append("        }").Append(_br)
                .Append("        return module;").Append(_br)
                .Append("    }").Append(_br)
                .Append(_br)
                .Append("    function define(obj, properties) {").Append(_br)
                .Append("        for (var name in properties) {").Append(_br)
                .Append("            if (!Object.prototype.hasOwnProperty.call(obj, name))").Append(_br)
                .Append("                Object.defineProperty(obj, name, { enumerable: true, get: properties[name], set: function () { throw new TypeError('Assignment to constant variable.'); } });").Append(_br)
                .Append("        }").Append(_br)
                .Append("    }").Append(_br)
                .Append(_br)
                .Append($"    {async}function finalize(imports) {{").Append(_br)
                .Append("        for (var i = 0; i < imports.length; i++)").Append(_br)
                .Append($"            {await}imports[i]();").Append(_br)
                .Append("        imports.length = 0;").Append(_br)
                .Append("    }").Append(_br)
                .Append(_br)
                .Append("    var imports = [];").Append(_br);

            for (int i = 0, n = rootModules.Length; i < n; i++)
            {
                ModuleData module = rootModules[i];
                sb.Append(' ', IndentSize).Append($"require(\"{module.Resource.IdEscaped}\", imports);").Append(_br);
            }

            sb.Append(' ', IndentSize).Append("finalize(imports);").Append(_br);
            sb.Append("})({");

            var separator = string.Empty;
            foreach (ModuleData module in Modules.Values)
            {
                sb.Append(separator).Append(_br)
                    .Append(' ', IndentSize).Append($"\"{module.Resource.IdEscaped}\": function ({RequireId}) {{").Append(_br);

                if (_developmentMode)
                {
                    var index = sb.Length;
                    sb.Append('/').Append('*', 3)
                        .Append($" MODULE: {module.Resource.Url.ToString()} ")
                        .Append('*', Math.Max(78 - (sb.Length - index), 0)).Append('*').Append('/')
                        .Append(_br);
                }

                sb.Append(module.Content).Append(_br);

                if (_developmentMode)
                    sb.Append('/').Append('*', 78).Append('/').Append(_br);

                sb.Append(' ', IndentSize).Append('}');

                separator = ",";
            }

            sb.Append(_br).Append("});").Append(_br);

            var imports = new HashSet<AbstractionFile>();

            foreach ((FileModuleResource fileResource, (bool isRoot, _)) in Files)
                if (!isRoot)
                    imports.Add(fileResource.ModuleFile);

            return new ModuleBundlingResult(sb.ToString(), imports);
        }
    }
}
