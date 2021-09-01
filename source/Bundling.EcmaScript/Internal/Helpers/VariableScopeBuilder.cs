using System;
using System.Collections.Generic;
using Esprima.Ast;
using Esprima.Utils;

namespace Karambolo.AspNetCore.Bundling.EcmaScript.Internal.Helpers
{
    internal class VariableScopeBuilder : AstVisitor
    {
        private readonly Dictionary<Node, VariableScope> _variableScopes;
        private readonly Stack<VariableScope> _variableScopeStack;

        public VariableScopeBuilder(Dictionary<Node, VariableScope> variableScopes)
        {
            if (variableScopes == null)
                throw new ArgumentNullException(nameof(variableScopes));

            _variableScopes = variableScopes;
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
            _variableScopes.Add(variableScope.OriginatorNode, variableScope);
        }

        protected virtual VariableScope.GlobalBlock HandleInvalidImportDeclaration(ImportDeclaration importDeclaration, string defaultErrorMessage) =>
            throw new InvalidOperationException(defaultErrorMessage);

        // Esprima.NET allows import declarations in scopes other than the top level scope (bug?), so we need to ensure this manually.
        private VariableScope.GlobalBlock EnsureImportDeclarationScope(ImportDeclaration importDeclaration) =>
            (CurrentVariableScope as VariableScope.GlobalBlock) ?? HandleInvalidImportDeclaration(importDeclaration, "Import declarations may only appear at top level of a module.");

        protected override void VisitArrowFunctionExpression(ArrowFunctionExpression arrowFunctionExpression)
        {
            BeginVariableScope(new VariableScope.Function(arrowFunctionExpression, CurrentVariableScope));

            VisitFunctionCore(arrowFunctionExpression);

            EndVariableScope();
        }

        protected override void VisitBlockStatement(BlockStatement blockStatement)
        {
            BeginVariableScope(new VariableScope.Block(blockStatement, CurrentVariableScope));

            base.VisitBlockStatement(blockStatement);

            EndVariableScope();
        }

        protected override void VisitCatchClause(CatchClause catchClause)
        {
            BeginVariableScope(new VariableScope.CatchClause(catchClause, CurrentVariableScope));

            var variableDeclarationVisitor = new VariableDeclarationVisitor<VariableScopeBuilder>(this,
                visitVariableIdentifier: (b, identifier) => ((VariableScope.CatchClause)b.CurrentVariableScope).AddParamDeclaration(identifier),
                visitRewritableExpression: (b, expression) => b.Visit(expression));

            variableDeclarationVisitor.VisitParam(catchClause);

            Visit(catchClause.Body);

            EndVariableScope();
        }

        private void VisitClassCore(IClass @class)
        {
            if (@class.SuperClass != null)
                Visit(@class.SuperClass);

            Visit(@class.Body);
        }

        protected override void VisitClassDeclaration(ClassDeclaration classDeclaration)
        {
            if (classDeclaration.Id != null)
                ((VariableScope.BlockBase)CurrentVariableScope).AddClassDeclaration(classDeclaration.Id);

            BeginVariableScope(new VariableScope.Class(classDeclaration, CurrentVariableScope));

            VisitClassCore(classDeclaration);

            EndVariableScope();
        }

        protected override void VisitClassExpression(ClassExpression classExpression)
        {
            // Class expression's name is not available the enclosing scope.

            BeginVariableScope(new VariableScope.Class(classExpression, CurrentVariableScope));

            VisitClassCore(classExpression);

            EndVariableScope();
        }

        protected override void VisitForInStatement(ForInStatement forInStatement)
        {
            var hasDeclaration = forInStatement.Left is VariableDeclaration;
            if (hasDeclaration)
                BeginVariableScope(new VariableScope.DeclaratorStatement(forInStatement, CurrentVariableScope));

            base.VisitForInStatement(forInStatement);

            if (hasDeclaration)
                EndVariableScope();
        }

        protected override void VisitForOfStatement(ForOfStatement forOfStatement)
        {
            var hasDeclaration = forOfStatement.Left is VariableDeclaration;
            if (hasDeclaration)
                BeginVariableScope(new VariableScope.DeclaratorStatement(forOfStatement, CurrentVariableScope));

            base.VisitForOfStatement(forOfStatement);

            if (hasDeclaration)
                EndVariableScope();
        }

        protected override void VisitForStatement(ForStatement forStatement)
        {
            var hasDeclaration = forStatement.Init is VariableDeclaration;
            if (hasDeclaration)
                BeginVariableScope(new VariableScope.DeclaratorStatement(forStatement, CurrentVariableScope));

            base.VisitForStatement(forStatement);

            if (hasDeclaration)
                EndVariableScope();
        }

        private void VisitFunctionCore(IFunction function)
        {
            var variableDeclarationVisitor = new VariableDeclarationVisitor<VariableScopeBuilder>(this,
                visitVariableIdentifier: (b, identifier) => ((VariableScope.Function)b.CurrentVariableScope).AddParamDeclaration(identifier),
                visitRewritableExpression: (b, expression) => b.Visit(expression));

            variableDeclarationVisitor.VisitParams(function);

            Visit(function.Body);
        }

        protected override void VisitFunctionDeclaration(FunctionDeclaration functionDeclaration)
        {
            if (functionDeclaration.Id != null)
                ((VariableScope.BlockBase)CurrentVariableScope).AddFunctionDeclaration(functionDeclaration.Id);

            BeginVariableScope(new VariableScope.Function(functionDeclaration, CurrentVariableScope));

            VisitFunctionCore(functionDeclaration);

            EndVariableScope();
        }

        protected override void VisitFunctionExpression(IFunction function)
        {
            // Function expression's name is not available in the enclosing scope.

            BeginVariableScope(new VariableScope.Function((FunctionExpression)function, CurrentVariableScope));

            VisitFunctionCore(function);

            EndVariableScope();
        }

        protected override void VisitImportDeclaration(ImportDeclaration importDeclaration)
        {
            VariableScope.GlobalBlock globalScope = EnsureImportDeclarationScope(importDeclaration);

            ref readonly NodeList<ImportDeclarationSpecifier> specifiers = ref importDeclaration.Specifiers;
            for (var i = 0; i < specifiers.Count; i++)
                globalScope.AddImportDeclaration(specifiers[i].Local);

            Visit(importDeclaration.Source);
        }

        protected override void VisitProgram(Program program)
        {
            _variableScopes.Clear();
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
        }

        protected override void VisitVariableDeclaration(VariableDeclaration variableDeclaration)
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
        }

        private static Exception UnknownNodeError(Node node)
        {
            return new NotSupportedException($"Nodes of type {node.Type} are not supported.");
        }
    }
}
