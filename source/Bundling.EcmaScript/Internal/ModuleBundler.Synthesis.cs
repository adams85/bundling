using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Esprima;
using Esprima.Ast;
using Karambolo.AspNetCore.Bundling.Internal;
using Karambolo.AspNetCore.Bundling.Internal.Helpers;
using Microsoft.Extensions.Primitives;

namespace Karambolo.AspNetCore.Bundling.EcmaScript.Internal
{
    internal partial class ModuleBundler
    {
        private class DescendingRangeComparer : IComparer<Range>
        {
            public static readonly DescendingRangeComparer Instance = new DescendingRangeComparer();

            private DescendingRangeComparer() { }

            public int Compare(Range x, Range y)
            {
                return y.Start.CompareTo(x.Start);
            }
        }

        private class RewriteModuleLocals
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

        private static ModuleBundlingErrorException CreateRewriteError(ModuleData module, in Position position, string reason)
        {
            return new ModuleBundlingErrorException($"Failed to rewrite file {module.FilePath} provided by {GetFileProviderHint(module.File)}. Error at {position}: " + reason);
        }

        private static StringSegment GetContentSegment(string content, in Range range)
        {
            return new StringSegment(content, range.Start, range.End - range.Start);
        }

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

        private static string GetModuleName(int uniqueId)
        {
            return ModuleIdPrefix + uniqueId;
        }

        private static string GetModuleVariableName(string moduleRef, string localName)
        {
            return moduleRef + "." + localName;
        }

        private void AppendImports(StringBuilder sb, ModuleData module)
        {
            if (_developmentMode)
            {
                sb.Append(_br);
                sb.Append("/* Imports */").Append(_br);
            }

            foreach (KeyValuePair<ModuleFile, string> moduleRef in module.ModuleRefs)
                sb.Append("var ").Append(moduleRef.Value).Append(" = ").Append(RequireId).Append('(')
                    .Append('"').Append(GetFileProviderPrefix(moduleRef.Key)).Append(moduleRef.Key.FilePath).Append('"').Append(')').Append(';').Append(_br);
        }

        private StringBuilder AppendNamedExportDefinition(StringBuilder sb, string exportName, string localExpression)
        {
            return sb.Append(RequireId).Append('.').Append(RequireDefineId).Append('(')
                .Append(ExportsId).Append(", ")
                .Append('"').Append(exportName).Append('"').Append(", ")
                .Append("function() { return ").Append(localExpression).Append("; }").Append(')');
        }

        private void AppendExpressionExportDefinition(StringBuilder sb, DefaultExpressionExportData export, string content)
        {
            Range range = export.Expression.Range;

            sb.Append(ExportsId).Append('[').Append('"').Append(export.ExportName).Append('"').Append(']').Append(" = ");
            if (content[range.Start] != '(')
                sb.Append('(').Append(content, range.Start, range.End - range.Start).Append(')');
            else
                sb.Append(content, range.Start, range.End - range.Start);
        }

        private void AppendExports(StringBuilder sb, ModuleData module, Dictionary<string, ExportData> exports)
        {
            if (_developmentMode)
            {
                sb.Append(_br);
                sb.Append("/* Exports */").Append(_br);
            }

            foreach (ExportData export in exports.Values)
            {
                switch (export)
                {
                    case DefaultExpressionExportData defaultExpressionExport:
                        AppendExpressionExportDefinition(sb, defaultExpressionExport, module.Content);
                        break;
                    case ReexportData reexport:
                        AppendNamedExportDefinition(sb, reexport.ExportName, GetModuleVariableName(module.ModuleRefs[reexport.ModuleFile], reexport.LocalName));
                        break;
                    case NamedExportData namedExport:
                        AppendNamedExportDefinition(sb, namedExport.ExportName, namedExport.LocalName);
                        break;
                }
                sb.Append(';').Append(_br);
            }
        }

        private void ExpandExports(Dictionary<string, ExportData> exports, HashSet<ModuleData> visitedModules, ModuleData module, ModuleFile moduleFile = null)
        {
            for (int i = 0, n = module.ExportsRaw.Count; i < n; i++)
                switch (module.ExportsRaw[i])
                {
                    // wildcard re-exports (export * from '...';)
                    case ReexportData reexport when reexport.ExportName == null:
                        ModuleData reexportedModule = _modules[reexport.ModuleFile];

                        if (moduleFile == null)
                            visitedModules.Clear();

                        if (visitedModules.Add(reexportedModule))
                            ExpandExports(exports, visitedModules, reexportedModule, moduleFile ?? reexport.ModuleFile);
                        break;
                    // locals and named re-exports when expanding a wildcard re-export
                    case NamedExportData namedExport when moduleFile != null:
                        exports[namedExport.ExportName] = new ReexportData(moduleFile, namedExport.ExportName, namedExport.ExportName);
                        break;
                    // rest
                    case ExportData export:
                        // default exports skipped when expanding a wildcard re-export
                        if (moduleFile == null || export.ExportName != DefaultExportName)
                            exports[export.ExportName] = export;
                        break;
                }
        }

        private RewriteModuleLocals RewriteModule(ModuleData module, ParallelLoopState loopState, RewriteModuleLocals locals)
        {
            locals.Reset();

            Dictionary<string, ExportData> exports = locals.Exports;
            SortedDictionary<Range, StringSegment> substitutions = locals.Substitutions;
            StringBuilder sb = locals.StringBuilder;

            sb.Append("'use strict';").Append(_br);

            // require modules

            if (module.ModuleRefs.Count > 0)
                AppendImports(sb, module);

            // define exports

            ExpandExports(exports, locals.VisitedModules, module);

            if (exports.Count > 0)
                AppendExports(sb, module, exports);

            // substitutions

            new SubstitutionCollector(module, substitutions).Collect();

            sb.Append(_br);
            int offset = sb.Length;

            sb.Append(module.Content);
            foreach (KeyValuePair<Range, StringSegment> substitution in substitutions)
                ReplaceRange(sb, module.Content, offset, substitution.Key, substitution.Value);

            module.Content = sb.ToString();

            return locals;
        }

        private ModuleBundlingResult BuildResult(ModuleFile[] rootModules)
        {
            var sb = new StringBuilder(
$@"(function (modules) {{
    var moduleCache = {{}};

    function {RequireId}(moduleId) {{
        var module = moduleCache[moduleId];
        if (!module) {{
            var module = {{
                id: moduleId,
                exports: {{}}
            }};

            modules[moduleId].call(module.exports, {RequireId}, module.exports);
            moduleCache[moduleId] = module;
        }}
        return module.exports;
    }}

    {RequireId}.d = function (exports, name, getter) {{
        if (!Object.prototype.hasOwnProperty.call(exports, name))
            Object.defineProperty(exports, name, {{ enumerable: true, get: getter }});
    }};

");

            for (int i = 0, n = rootModules.Length; i < n; i++)
            {
                ModuleFile moduleFile = rootModules[i];
                sb.Append(' ', 4).Append($"{RequireId}(\"{GetFileProviderPrefix(moduleFile)}{moduleFile.FilePath}\");").Append(_br);
            }

            sb
                .Append("})({").Append(_br);

            foreach (ModuleData module in _modules.Values)
            {
                if (_developmentMode)
                    sb.Append(' ', 4).Append($"/*** Module: {GetFileProviderHint(module.File)}:{module.FilePath} *** /").Append(module.FilePath).Append(" ***/").Append(_br);

                sb.Append(' ', 4).Append($"\"{GetFileProviderPrefix(module.File)}{module.FilePath}\": function ({RequireId}, {ExportsId}) {{").Append(_br);

                if (_developmentMode)
                    sb.Append("/* ").Append("<").Append('-', 4).Append(" */").Append(_br);

                sb.Append(module.Content).Append(_br);

                if (_developmentMode)
                    sb.Append("/* ").Append('-', 4).Append(">").Append(" */");
                        
                sb.Append(" },").Append(_br);
            }

            sb
                .Append("});").Append(_br);

            var imports = new HashSet<AbstractionFile>();

            foreach (ModuleFile moduleFile in _modules.Keys)
                if (!rootModules.Contains(moduleFile))
                {
                    moduleFile.Content = null;
                    imports.Add(moduleFile);
                }

            return new ModuleBundlingResult(sb.ToString(), imports);
        }
    }
}
