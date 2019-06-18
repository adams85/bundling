using System;
using System.Collections.Generic;
using Esprima.Ast;
using Esprima.Utils;
using Microsoft.Extensions.Primitives;

namespace Karambolo.AspNetCore.Bundling.EcmaScript.Internal
{
    internal partial class ModuleBundler
    {
        private delegate void SubstitutionAdjuster(ref StringSegment value);

        private class SubstitutionCollector : AstVisitor
        {
            private readonly ModuleData _module;
            private readonly SortedDictionary<Range, StringSegment> _substitutions;
            private readonly Stack<HashSet<string>> _nameScopeStack;

            public SubstitutionCollector(ModuleData module, SortedDictionary<Range, StringSegment> substitutions)
            {
                _module = module;
                _substitutions = substitutions;
                _nameScopeStack = new Stack<HashSet<string>>();
                _nameScopeStack.Push(new HashSet<string>());
            }

            private HashSet<string> CurrentNameScope => _nameScopeStack.Peek();

            private void BeginNameScope(NodeList<INode> @params)
            {
                HashSet<string> currentNameScope = CurrentNameScope;

                if (@params.Count > 0)
                {
                    currentNameScope = new HashSet<string>(currentNameScope);

                    for (int i = 0, n = @params.Count; i < n; i++)
                        switch (@params[i])
                        {
                            case Identifier identifier:
                                currentNameScope.Add(identifier.Name);
                                break;
                            case AssignmentPattern assignmentPattern when assignmentPattern.Left is Identifier identifier:
                                currentNameScope.Add(identifier.Name);
                                break;
                            case RestElement restElement when restElement.Argument is Identifier identifier:
                                currentNameScope.Add(identifier.Name);
                                break;
                        }
                }

                _nameScopeStack.Push(currentNameScope);
            }

            private void EndNameScope()
            {
                _nameScopeStack.Pop();
            }

            private void AddSubstitution(Identifier identifier, SubstitutionAdjuster adjust)
            {
                if (CurrentNameScope.Contains(identifier.Name) ||
                    !_module.Imports.TryGetValue(identifier.Name, out ImportData import))
                    return;

                StringSegment value;
                switch (import)
                {
                    case NamedImportData namedImport:
                        value = GetModuleVariableName(_module.ModuleRefs[import.ModuleFile], namedImport.ImportName);
                        break;
                    case NamespaceImportData namespaceImport:
                        value = _module.ModuleRefs[import.ModuleFile];
                        break;
                    default:
                        return;
                }

                adjust(ref value);
                _substitutions.Add(identifier.Range, value);
            }

            public void Collect()
            {
                Visit(_module.Ast);
            }

            protected override void VisitArrayPattern(ArrayPattern arrayPattern) { }

            protected override void VisitArrayExpression(ArrayExpression arrayExpression)
            {
                for (int i = 0, n = arrayExpression.Elements.Count; i < n; i++)
                    Visit(arrayExpression.Elements[i]);
            }

            protected override void VisitArrowFunctionExpression(ArrowFunctionExpression arrowFunctionExpression)
            {
                // name, params skipped

                BeginNameScope(arrowFunctionExpression.Params);
                Visit(arrowFunctionExpression.Body);
                EndNameScope();
            }

            protected override void VisitArrowParameterPlaceHolder(ArrowParameterPlaceHolder arrowParameterPlaceHolder)
            {
                // ArrowParameterPlaceHolder nodes never appear in the final tree and 
                // only used during the construction of a tree.
            }

            protected override void VisitAssignmentExpression(AssignmentExpression assignmentExpression)
            {
                // left side skipped (imports are read-only)
                Visit(assignmentExpression.Right);
            }

            protected override void VisitAssignmentPattern(AssignmentPattern assignmentPattern)
            {
                // left side skipped (imports are read-only)
                Visit(assignmentPattern.Right);
            }

            protected override void VisitBinaryExpression(BinaryExpression binaryExpression)
            {
                Visit(binaryExpression.Left);
                Visit(binaryExpression.Right);
            }

            protected override void VisitBlockStatement(BlockStatement blockStatement)
            {
                for (int i = 0, n = blockStatement.Body.Count; i < n; i++)
                    Visit(blockStatement.Body[i]);
            }

            protected override void VisitBreakStatement(BreakStatement breakStatement) { }

            protected override void VisitCallExpression(CallExpression callExpression)
            {
                if (!callExpression.Cached)
                    for (int i = 0, n = callExpression.Arguments.Count; i < n; i++)
                        Visit(callExpression.Arguments[i]);

                Visit(callExpression.Callee);
            }

            protected override void VisitCatchClause(CatchClause catchClause)
            {
                // error variable identifier skipped
                Visit(catchClause.Body);
            }

            protected override void VisitClassBody(ClassBody classBody)
            {
                for (int i = 0, n = classBody.Body.Count; i < n; i++)
                    Visit(classBody.Body[i]);
            }

            protected override void VisitClassDeclaration(ClassDeclaration classDeclaration)
            {
                // class name identifier skipped
                if (classDeclaration.SuperClass != null)
                    Visit(classDeclaration.SuperClass);

                Visit(classDeclaration.Body);
            }

            protected override void VisitClassExpression(ClassExpression classExpression)
            {
                // class name identifier skipped
                if (classExpression.SuperClass != null)
                    Visit(classExpression.SuperClass);

                Visit(classExpression.Body);
            }

            protected override void VisitConditionalExpression(ConditionalExpression conditionalExpression)
            {
                Visit(conditionalExpression.Test);
                Visit(conditionalExpression.Consequent);
                Visit(conditionalExpression.Alternate);
            }

            protected override void VisitContinueStatement(ContinueStatement continueStatement) { }

            protected override void VisitDebuggerStatement(DebuggerStatement debuggerStatement) { }

            protected override void VisitDoWhileStatement(DoWhileStatement doWhileStatement)
            {
                Visit(doWhileStatement.Body);
                Visit(doWhileStatement.Test);
            }

            protected override void VisitEmptyStatement(EmptyStatement emptyStatement) { }

            protected override void VisitExportAllDeclaration(ExportAllDeclaration exportAllDeclaration)
            {
                _substitutions.Add(exportAllDeclaration.Range, StringSegment.Empty);
            }

            protected override void VisitExportDefaultDeclaration(ExportDefaultDeclaration exportDefaultDeclaration)
            {
                switch (exportDefaultDeclaration.Declaration)
                {
                    case FunctionDeclaration functionDeclaration when functionDeclaration.Id != null:
                        _substitutions.Add(exportDefaultDeclaration.Range, GetContentSegment(_module.Content, functionDeclaration.Range));
                        break;
                    case ClassDeclaration classDeclaration when classDeclaration.Id != null:
                        _substitutions.Add(exportDefaultDeclaration.Range, GetContentSegment(_module.Content, classDeclaration.Range));
                        break;
                    default:
                        _substitutions.Add(exportDefaultDeclaration.Range, StringSegment.Empty);
                        break;
                }
            }

            protected override void VisitExportNamedDeclaration(ExportNamedDeclaration exportNamedDeclaration)
            {
                if (exportNamedDeclaration.Source != null)
                    _substitutions.Add(exportNamedDeclaration.Range, StringSegment.Empty);
                else if (exportNamedDeclaration.Declaration != null)
                    _substitutions.Add(exportNamedDeclaration.Range, GetContentSegment(_module.Content, exportNamedDeclaration.Declaration.Range));
                else
                    _substitutions.Add(exportNamedDeclaration.Range, StringSegment.Empty);
            }

            protected override void VisitImportDeclaration(ImportDeclaration importDeclaration)
            {
                _substitutions.Add(importDeclaration.Range, StringSegment.Empty);
            }

            protected override void VisitExportSpecifier(ExportSpecifier exportSpecifier) { }

            protected override void VisitExpressionStatement(ExpressionStatement expressionStatement)
            {
                Visit(expressionStatement.Expression);
            }

            protected override void VisitForInStatement(ForInStatement forInStatement)
            {
                // left side skipped (imports are read-only)
                Visit(forInStatement.Right);
                Visit(forInStatement.Body);
            }

            protected override void VisitForOfStatement(ForOfStatement forOfStatement)
            {
                // left side skipped (imports are read-only)
                Visit(forOfStatement.Right);
                Visit(forOfStatement.Body);
            }

            protected override void VisitForStatement(ForStatement forStatement)
            {
                if (forStatement.Init != null)
                    Visit(forStatement.Init);

                if (forStatement.Test != null)
                    Visit(forStatement.Test);

                if (forStatement.Update != null)
                    Visit(forStatement.Update);

                Visit(forStatement.Body);
            }

            protected override void VisitFunctionDeclaration(FunctionDeclaration functionDeclaration)
            {
                // name, params skipped
                BeginNameScope(functionDeclaration.Params);
                Visit(functionDeclaration.Body);
                EndNameScope();
            }

            protected override void VisitFunctionExpression(IFunction function)
            {
                // name, params skipped
                BeginNameScope(function.Params);
                Visit(function.Body);
                EndNameScope();
            }

            protected override void VisitIdentifier(Identifier identifier)
            {
                AddSubstitution(identifier, delegate { });
            }

            protected override void VisitIfStatement(IfStatement ifStatement)
            {
                Visit(ifStatement.Test);
                Visit(ifStatement.Consequent);
                if (ifStatement.Alternate != null)
                    Visit(ifStatement.Alternate);
            }

            protected override void VisitImportDefaultSpecifier(ImportDefaultSpecifier importDefaultSpecifier) { }

            protected override void VisitImportNamespaceSpecifier(ImportNamespaceSpecifier importNamespaceSpecifier) { }

            protected override void VisitImportSpecifier(ImportSpecifier importSpecifier) { }

            protected override void VisitLabeledStatement(LabeledStatement labeledStatement)
            {
                // label identifier skipped
                Visit(labeledStatement.Body);
            }

            protected override void VisitLiteral(Literal literal) { }

            protected override void VisitLogicalExpression(BinaryExpression binaryExpression)
            {
                Visit(binaryExpression.Left);
                Visit(binaryExpression.Right);
            }

            protected override void VisitMemberExpression(MemberExpression memberExpression)
            {
                // property skipped (only roots of a member access chain can be an import)
                Visit(memberExpression.Object);
            }

            protected override void VisitMetaProperty(MetaProperty metaProperty) { }

            protected override void VisitMethodDefinition(MethodDefinition methodDefinitions)
            {
                // key skipped if not computed (e.g. "class C { [var]() {} }")
                if (methodDefinitions.Computed)
                    Visit(methodDefinitions.Key);

                Visit(methodDefinitions.Value);
            }

            protected override void VisitNewExpression(NewExpression newExpression)
            {
                for (int i = 0, n = newExpression.Arguments.Count; i < n; i++)
                    Visit(newExpression.Arguments[i]);

                Visit(newExpression.Callee);
            }

            protected override void VisitObjectExpression(ObjectExpression objectExpression)
            {
                for (int i = 0, n = objectExpression.Properties.Count; i < n; i++)
                    Visit(objectExpression.Properties[i]);
            }

            protected override void VisitObjectPattern(ObjectPattern objectPattern) { }

            protected override void VisitProperty(Property property)
            {
                // shorthand properties need special care
                if (property.Shorthand && !property.Method && property.Value is Identifier identifier)
                {
                    AddSubstitution(identifier, delegate (ref StringSegment value) { value = identifier.Name + ": " + value; });
                    return;
                }

                // key skipped if not computed (e.g. "{ [var]: 0 }")
                if (property.Computed)
                    Visit(property.Key);

                Visit(property.Value);
            }

            protected override void VisitRestElement(RestElement restElement) { }

            protected override void VisitReturnStatement(ReturnStatement returnStatement)
            {
                Visit(returnStatement.Argument);
            }

            protected override void VisitSequenceExpression(SequenceExpression sequenceExpression)
            {
                for (int i = 0, n = sequenceExpression.Expressions.Count; i < n; i++)
                    Visit(sequenceExpression.Expressions[i]);
            }

            protected override void VisitSpreadElement(SpreadElement spreadElement)
            {
                Visit(spreadElement.Argument);
            }

            protected override void VisitProgram(Esprima.Ast.Program program)
            {
                for (int i = 0, n = program.Body.Count; i < n; i++)
                    Visit(program.Body[i]);
            }

            protected override void VisitSuper(Super super) { }

            protected override void VisitSwitchCase(SwitchCase switchCase)
            {
                if (switchCase.Test != null)
                    Visit(switchCase.Test);

                for (int i = 0, n = switchCase.Consequent.Count; i < n; i++)
                    Visit(switchCase.Consequent[i]);
            }

            protected override void VisitSwitchStatement(SwitchStatement switchStatement)
            {
                VisitExpression(switchStatement.Discriminant);

                for (int i = 0, n = switchStatement.Cases.Count; i < n; i++)
                    Visit(switchStatement.Cases[i]);
            }

            protected override void VisitTaggedTemplateExpression(TaggedTemplateExpression taggedTemplateExpression)
            {
                Visit(taggedTemplateExpression.Tag);
                Visit(taggedTemplateExpression.Quasi);
            }

            protected override void VisitTemplateElement(TemplateElement templateElement) { }

            protected override void VisitTemplateLiteral(TemplateLiteral templateLiteral)
            {
                // literal parts (quasis) are irrelevant
                for (int i = 0, n = templateLiteral.Expressions.Count; i < n; i++)
                    Visit(templateLiteral.Expressions[i]);
            }

            protected override void VisitThisExpression(ThisExpression thisExpression) { }

            protected override void VisitThrowStatement(ThrowStatement throwStatement)
            {
                Visit(throwStatement.Argument);
            }

            protected override void VisitTryStatement(TryStatement tryStatement)
            {
                Visit(tryStatement.Block);

                if (tryStatement.Handler != null)
                    Visit(tryStatement.Handler);

                if (tryStatement.Finalizer != null)
                    Visit(tryStatement.Finalizer);
            }

            protected override void VisitUnaryExpression(UnaryExpression unaryExpression)
            {
                // operators ++, -- and delete skipped (imports are read-only)
                switch (unaryExpression.Operator)
                {
                    case UnaryOperator.Increment:
                    case UnaryOperator.Decrement:
                    case UnaryOperator.Delete:
                        return;
                }

                Visit(unaryExpression.Argument);
            }

            protected override void VisitUnknownNode(INode node)
            {
                Range range = node.Range;
                throw CreateRewriteError(_module, node.Location.Start, $"'{_module.Content.Substring(range.Start, range.End - range.Start)}' is not supported currently.");
            }

            protected override void VisitUpdateExpression(UpdateExpression updateExpression) { }

            protected override void VisitVariableDeclaration(VariableDeclaration variableDeclaration)
            {
                for (int i = 0, n = variableDeclaration.Declarations.Count; i < n; i++)
                    Visit(variableDeclaration.Declarations[i]);
            }

            protected override void VisitVariableDeclarator(VariableDeclarator variableDeclarator)
            {
                // id skipped
                Visit(variableDeclarator.Init);
            }

            protected override void VisitWhileStatement(WhileStatement whileStatement)
            {
                Visit(whileStatement.Test);
                Visit(whileStatement.Body);
            }

            protected override void VisitWithStatement(WithStatement withStatement)
            {
                // modules are always in strict mode and that doesn't allow with statements
                throw CreateRewriteError(_module, withStatement.Location.Start, "With statements are not supported in ES6 modules.");
            }

            protected override void VisitYieldExpression(YieldExpression yieldExpression)
            {
                Visit(yieldExpression.Argument);
            }
        }
    }
}
