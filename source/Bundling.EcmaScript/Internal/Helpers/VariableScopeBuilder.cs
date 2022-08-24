using System;
using Esprima.Ast;
using Esprima.Utils;

namespace Karambolo.AspNetCore.Bundling.EcmaScript.Internal.Helpers
{
    internal class VariableScopeBuilder : AstVisitor
    {
        protected readonly struct Snapshot
        {
            private readonly VariableScope _currentVariableScope;

            public Snapshot(VariableScopeBuilder builder)
            {
                _currentVariableScope = builder._currentVariableScope;
            }

            public void Restore(VariableScopeBuilder builder)
            {
                builder._currentVariableScope = _currentVariableScope;
            }
        }

        private readonly Action<Node, VariableScope> _recordVariableScope;
        private VariableScope _currentVariableScope;

        public VariableScopeBuilder() : this((node, scope) => node.AssociatedData = scope) { }

        public VariableScopeBuilder(Action<Node, VariableScope> recordVariableScope)
        {
            _recordVariableScope = recordVariableScope ?? throw new ArgumentNullException(nameof(recordVariableScope));
        }

        protected VariableScope CurrentVariableScope => _currentVariableScope;

        protected void BeginVariableScope(VariableScope variableScope, out Snapshot snapshot)
        {
            snapshot = new Snapshot(this);
            _currentVariableScope = variableScope;
        }

        protected void EndVariableScope(in Snapshot snapshot)
        {
            _currentVariableScope.FinalizeScope();
            _recordVariableScope(_currentVariableScope.OriginatorNode, _currentVariableScope);
            snapshot.Restore(this);
        }

        protected virtual VariableScope.GlobalBlock HandleInvalidImportDeclaration(ImportDeclaration importDeclaration, string defaultErrorMessage) =>
            throw new InvalidOperationException(defaultErrorMessage);

        // Esprima.NET allows import declarations in scopes other than the top level scope (bug?), so we need to ensure this manually.
        private VariableScope.GlobalBlock EnsureImportDeclarationScope(ImportDeclaration importDeclaration) =>
            (_currentVariableScope as VariableScope.GlobalBlock) ?? HandleInvalidImportDeclaration(importDeclaration, "Import declarations may only appear at top level of a module.");

        protected override object VisitArrowFunctionExpression(ArrowFunctionExpression arrowFunctionExpression)
        {
            BeginVariableScope(new VariableScope.Function(arrowFunctionExpression, _currentVariableScope), out Snapshot snapshot);

            VisitFunctionCore(arrowFunctionExpression);

            EndVariableScope(in snapshot);

            return arrowFunctionExpression;
        }

        protected override object VisitBlockStatement(BlockStatement blockStatement)
        {
            BeginVariableScope(new VariableScope.Block(blockStatement, _currentVariableScope), out Snapshot snapshot);

            base.VisitBlockStatement(blockStatement);

            EndVariableScope(in snapshot);

            return blockStatement;
        }

        protected override object VisitCatchClause(CatchClause catchClause)
        {
            BeginVariableScope(new VariableScope.CatchClause(catchClause, _currentVariableScope), out Snapshot snapshot);

            var variableDeclarationVisitor = new VariableDeclarationVisitor<VariableScopeBuilder>(this,
                visitVariableIdentifier: (b, identifier) => ((VariableScope.CatchClause)b._currentVariableScope).AddParamDeclaration(identifier),
                visitRewritableExpression: (b, expression) => b.Visit(expression));

            variableDeclarationVisitor.VisitCatchClauseParam(catchClause);

            Visit(catchClause.Body);

            EndVariableScope(in snapshot);

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
            ref readonly NodeList<Decorator> decorators = ref classDeclaration.Decorators;
            for (var i = 0; i < decorators.Count; i++)
            {
                Visit(decorators[i]);
            }

            if (classDeclaration.Id != null)
                ((VariableScope.BlockBase)_currentVariableScope).AddClassDeclaration(classDeclaration.Id);

            BeginVariableScope(new VariableScope.Class(classDeclaration, _currentVariableScope), out Snapshot snapshot);

            VisitClassCore(classDeclaration);

            EndVariableScope(in snapshot);

            return classDeclaration;
        }

        protected override object VisitClassExpression(ClassExpression classExpression)
        {
            ref readonly NodeList<Decorator> decorators = ref classExpression.Decorators;
            for (var i = 0; i < decorators.Count; i++)
            {
                Visit(decorators[i]);
            }

            // Class expression's name is not available the enclosing scope.

            BeginVariableScope(new VariableScope.Class(classExpression, _currentVariableScope), out Snapshot snapshot);

            VisitClassCore(classExpression);

            EndVariableScope(in snapshot);

            return classExpression;
        }

        protected override object VisitForInStatement(ForInStatement forInStatement)
        {
            if (!(forInStatement.Left is VariableDeclaration))
                return base.VisitForInStatement(forInStatement);

            BeginVariableScope(new VariableScope.VariableDeclaratorStatement(forInStatement, _currentVariableScope), out Snapshot snapshot);

            base.VisitForInStatement(forInStatement);

            EndVariableScope(in snapshot);

            return forInStatement;
        }

        protected override object VisitForOfStatement(ForOfStatement forOfStatement)
        {
            if (!(forOfStatement.Left is VariableDeclaration))
                return base.VisitForOfStatement(forOfStatement);

            BeginVariableScope(new VariableScope.VariableDeclaratorStatement(forOfStatement, _currentVariableScope), out Snapshot snapshot);

            base.VisitForOfStatement(forOfStatement);

            EndVariableScope(in snapshot);

            return forOfStatement;
        }

        protected override object VisitForStatement(ForStatement forStatement)
        {
            if (!(forStatement.Init is VariableDeclaration))
                return base.VisitForStatement(forStatement);

            BeginVariableScope(new VariableScope.VariableDeclaratorStatement(forStatement, _currentVariableScope), out Snapshot snapshot);

            base.VisitForStatement(forStatement);

            EndVariableScope(in snapshot);

            return forStatement;
        }

        private void VisitFunctionCore(IFunction function)
        {
            var variableDeclarationVisitor = new VariableDeclarationVisitor<VariableScopeBuilder>(this,
                visitVariableIdentifier: (b, identifier) => ((VariableScope.Function)b._currentVariableScope).AddParamDeclaration(identifier),
                visitRewritableExpression: (b, expression) => b.Visit(expression));

            variableDeclarationVisitor.VisitFunctionParams(function);

            Visit(function.Body);
        }

        protected override object VisitFunctionDeclaration(FunctionDeclaration functionDeclaration)
        {
            if (functionDeclaration.Id != null)
                ((VariableScope.BlockBase)_currentVariableScope).AddFunctionDeclaration(functionDeclaration.Id);

            BeginVariableScope(new VariableScope.Function(functionDeclaration, _currentVariableScope), out Snapshot snapshot);

            VisitFunctionCore(functionDeclaration);

            EndVariableScope(in snapshot);

            return functionDeclaration;
        }

        protected override object VisitFunctionExpression(FunctionExpression functionExpression)
        {
            // Function expression's name is not available in the enclosing scope.

            BeginVariableScope(new VariableScope.Function(functionExpression, _currentVariableScope), out Snapshot snapshot);

            VisitFunctionCore(functionExpression);

            EndVariableScope(in snapshot);

            return functionExpression;
        }

        protected override object VisitImportDeclaration(ImportDeclaration importDeclaration)
        {
            VariableScope.GlobalBlock globalScope = EnsureImportDeclarationScope(importDeclaration);

            ref readonly NodeList<ImportDeclarationSpecifier> specifiers = ref importDeclaration.Specifiers;
            for (var i = 0; i < specifiers.Count; i++)
                globalScope.AddImportDeclaration(specifiers[i].Local);

            Visit(importDeclaration.Source);

            ref readonly NodeList<ImportAttribute> assertions = ref importDeclaration.Assertions;
            for (var i = 0; i < assertions.Count; i++)
            {
                Visit(assertions[i]);
            }

            return importDeclaration;
        }

        protected override object VisitProgram(Program program)
        {
            _currentVariableScope = null;

            VariableScope.GlobalBlock globalScope;

            switch (program)
            {
                case Module module:
                    globalScope = new VariableScope.GlobalBlock(module);
                    break;
                case Script script:
                    globalScope = new VariableScope.GlobalBlock(script);
                    break;
                default:
                    throw UnknownNodeError(program);
            }

            BeginVariableScope(globalScope, out Snapshot snapshot);

            base.VisitProgram(program);

            EndVariableScope(in snapshot);

            return program;
        }

        protected override object VisitStaticBlock(StaticBlock staticBlock)
        {
            BeginVariableScope(new VariableScope.ClassStaticBlock(staticBlock, _currentVariableScope), out Snapshot snapshot);

            base.VisitStaticBlock(staticBlock);

            EndVariableScope(in snapshot);

            return staticBlock;
        }

        protected override object VisitVariableDeclaration(VariableDeclaration variableDeclaration)
        {
            var variableDeclarationVisitor = new VariableDeclarationVisitor<(VariableScopeBuilder Builder, VariableDeclaration Declaration)>((this, variableDeclaration),
                visitVariableIdentifier: (s, identifier) => ((VariableScope.VariableDeclaratorBase)s.Builder._currentVariableScope).AddVariableDeclaration(identifier, s.Declaration.Kind),
                visitRewritableExpression: (s, expression) => s.Builder.Visit(expression));

            ref readonly NodeList<VariableDeclarator> declarations = ref variableDeclaration.Declarations;
            for (var i = 0; i < declarations.Count; i++)
            {
                VariableDeclarator variableDeclarator = declarations[i];

                variableDeclarationVisitor.VisitVariableDeclaratorId(variableDeclarator);

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
