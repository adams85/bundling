using System;
using System.Collections.Generic;
using Esprima.Ast;
using Esprima.Utils;

namespace Karambolo.AspNetCore.Bundling.EcmaScript.Internal.Helpers
{
    // TODO: eliminate stack?
    internal class VariableScopeBuilder : AstVisitor
    {
        private readonly Action<Node, VariableScope> _recordVariableScope;
        private readonly Stack<VariableScope> _variableScopeStack;

        public VariableScopeBuilder() : this((node, scope) => node.Data = scope) { }

        public VariableScopeBuilder(Action<Node, VariableScope> recordVariableScope)
        {
            _recordVariableScope = recordVariableScope ?? throw new ArgumentNullException(nameof(recordVariableScope));
            _variableScopeStack = new Stack<VariableScope>();
        }

        protected VariableScope CurrentVariableScope => _variableScopeStack.Peek();

        protected void BeginVariableScope(VariableScope scope)
        {
            _variableScopeStack.Push(scope);
        }

        protected void EndVariableScope()
        {
            VariableScope variableScope = _variableScopeStack.Pop();
            variableScope.FinalizeScope();
            _recordVariableScope(variableScope.OriginatorNode, variableScope);
        }

        protected virtual VariableScope.GlobalBlock HandleInvalidImportDeclaration(ImportDeclaration importDeclaration, string defaultErrorMessage) =>
            throw new InvalidOperationException(defaultErrorMessage);

        // Esprima.NET allows import declarations in scopes other than the top level scope (bug?), so we need to ensure this manually.
        private VariableScope.GlobalBlock EnsureImportDeclarationScope(ImportDeclaration importDeclaration) =>
            (CurrentVariableScope as VariableScope.GlobalBlock) ?? HandleInvalidImportDeclaration(importDeclaration, "Import declarations may only appear at top level of a module.");

        protected override object VisitArrowFunctionExpression(ArrowFunctionExpression arrowFunctionExpression)
        {
            BeginVariableScope(new VariableScope.Function(arrowFunctionExpression, CurrentVariableScope));

            VisitFunctionCore(arrowFunctionExpression);

            EndVariableScope();

            return arrowFunctionExpression;
        }

        protected override object VisitBlockStatement(BlockStatement blockStatement)
        {
            BeginVariableScope(new VariableScope.Block(blockStatement, CurrentVariableScope));

            base.VisitBlockStatement(blockStatement);

            EndVariableScope();

            return blockStatement;
        }

        protected override object VisitCatchClause(CatchClause catchClause)
        {
            BeginVariableScope(new VariableScope.CatchClause(catchClause, CurrentVariableScope));

            var variableDeclarationVisitor = new VariableDeclarationVisitor<VariableScopeBuilder>(this,
                visitVariableIdentifier: (b, identifier) => ((VariableScope.CatchClause)b.CurrentVariableScope).AddParamDeclaration(identifier),
                visitRewritableExpression: (b, expression) => b.Visit(expression));

            variableDeclarationVisitor.VisitParam(catchClause);

            Visit(catchClause.Body);

            EndVariableScope();

            return catchClause;
        }

        private void VisitClassCore(IClass @class)
        {
            if (@class.SuperClass != null)
                Visit(@class.SuperClass);

            Visit(@class.Body);
        }

        protected override object VisitClassDeclaration(ClassDeclaration classDeclaration)
        {
            if (classDeclaration.Id != null)
                ((VariableScope.BlockBase)CurrentVariableScope).AddClassDeclaration(classDeclaration.Id);

            BeginVariableScope(new VariableScope.Class(classDeclaration, CurrentVariableScope));

            VisitClassCore(classDeclaration);

            EndVariableScope();

            return classDeclaration;
        }

        protected override object VisitClassExpression(ClassExpression classExpression)
        {
            // Class expression's name is not available the enclosing scope.

            BeginVariableScope(new VariableScope.Class(classExpression, CurrentVariableScope));

            VisitClassCore(classExpression);

            EndVariableScope();

            return classExpression;
        }

        protected override object VisitForInStatement(ForInStatement forInStatement)
        {
            var hasDeclaration = forInStatement.Left is VariableDeclaration;
            if (hasDeclaration)
                BeginVariableScope(new VariableScope.DeclaratorStatement(forInStatement, CurrentVariableScope));

            base.VisitForInStatement(forInStatement);

            if (hasDeclaration)
                EndVariableScope();

            return forInStatement;
        }

        protected override object VisitForOfStatement(ForOfStatement forOfStatement)
        {
            var hasDeclaration = forOfStatement.Left is VariableDeclaration;
            if (hasDeclaration)
                BeginVariableScope(new VariableScope.DeclaratorStatement(forOfStatement, CurrentVariableScope));

            base.VisitForOfStatement(forOfStatement);

            if (hasDeclaration)
                EndVariableScope();

            return forOfStatement;
        }

        protected override object VisitForStatement(ForStatement forStatement)
        {
            var hasDeclaration = forStatement.Init is VariableDeclaration;
            if (hasDeclaration)
                BeginVariableScope(new VariableScope.DeclaratorStatement(forStatement, CurrentVariableScope));

            base.VisitForStatement(forStatement);

            if (hasDeclaration)
                EndVariableScope();

            return forStatement;
        }

        private void VisitFunctionCore(IFunction function)
        {
            var variableDeclarationVisitor = new VariableDeclarationVisitor<VariableScopeBuilder>(this,
                visitVariableIdentifier: (b, identifier) => ((VariableScope.Function)b.CurrentVariableScope).AddParamDeclaration(identifier),
                visitRewritableExpression: (b, expression) => b.Visit(expression));

            variableDeclarationVisitor.VisitParams(function);

            Visit(function.Body);
        }

        protected override object VisitFunctionDeclaration(FunctionDeclaration functionDeclaration)
        {
            if (functionDeclaration.Id != null)
                ((VariableScope.BlockBase)CurrentVariableScope).AddFunctionDeclaration(functionDeclaration.Id);

            BeginVariableScope(new VariableScope.Function(functionDeclaration, CurrentVariableScope));

            VisitFunctionCore(functionDeclaration);

            EndVariableScope();

            return functionDeclaration;
        }

        protected override object VisitFunctionExpression(FunctionExpression functionExpression)
        {
            // Function expression's name is not available in the enclosing scope.

            BeginVariableScope(new VariableScope.Function(functionExpression, CurrentVariableScope));

            VisitFunctionCore(functionExpression);

            EndVariableScope();

            return functionExpression;
        }

        protected override object VisitImportDeclaration(ImportDeclaration importDeclaration)
        {
            VariableScope.GlobalBlock globalScope = EnsureImportDeclarationScope(importDeclaration);

            ref readonly NodeList<ImportDeclarationSpecifier> specifiers = ref importDeclaration.Specifiers;
            for (var i = 0; i < specifiers.Count; i++)
                globalScope.AddImportDeclaration(specifiers[i].Local);

            Visit(importDeclaration.Source);

            return importDeclaration;
        }

        protected override object VisitProgram(Program program)
        {
            _variableScopeStack.Clear();

            VariableScope.Global globalScope;

            switch (program)
            {
                case Module module:
                    globalScope = new VariableScope.Global(module);
                    break;
                case Script script:
                    globalScope = new VariableScope.Global(script);
                    break;
                default:
                    throw UnknownNodeError(program);
            }

            BeginVariableScope(globalScope);
            BeginVariableScope(new VariableScope.GlobalBlock(program, globalScope));

            base.VisitProgram(program);

            EndVariableScope();
            EndVariableScope();

            return program;
        }

        protected override object VisitStaticBlock(StaticBlock staticBlock)
        {
            // TODO
            throw new NotImplementedException();
        }

        protected override object VisitVariableDeclaration(VariableDeclaration variableDeclaration)
        {
            var variableDeclarationVisitor = new VariableDeclarationVisitor<(VariableScopeBuilder Builder, VariableDeclaration Declaration)>((this, variableDeclaration),
                visitVariableIdentifier: (s, identifier) => ((VariableScope.StatementBase)s.Builder.CurrentVariableScope).AddVariableDeclaration(identifier, s.Declaration.Kind),
                visitRewritableExpression: (s, expression) => s.Builder.Visit(expression));

            ref readonly NodeList<VariableDeclarator> declarations = ref variableDeclaration.Declarations;
            for (var i = 0; i < declarations.Count; i++)
            {
                VariableDeclarator variableDeclarator = declarations[i];

                variableDeclarationVisitor.VisitId(variableDeclarator);

                if (variableDeclarator.Init != null)
                    Visit(variableDeclarator.Init);
            }

            return variableDeclaration;
        }

        private static Exception UnknownNodeError(Node node)
        {
            return new NotSupportedException($"Nodes of type {node.Type} are not supported.");
        }
    }
}
