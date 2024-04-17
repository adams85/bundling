using System;
using Acornima;
using Acornima.Ast;

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

        public VariableScopeBuilder() : this((node, scope) => node.UserData = scope) { }

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

        protected override object VisitArrowFunctionExpression(ArrowFunctionExpression node)
        {
            BeginVariableScope(new VariableScope.Function(node, _currentVariableScope), out Snapshot snapshot);

            VisitFunctionCore(node);

            EndVariableScope(in snapshot);

            return node;
        }

        protected override object VisitBlockStatement(BlockStatement node)
        {
            BeginVariableScope(new VariableScope.Block((NestedBlockStatement)node, _currentVariableScope), out Snapshot snapshot);

            base.VisitBlockStatement(node);

            EndVariableScope(in snapshot);

            return node;
        }

        protected override object VisitCatchClause(CatchClause node)
        {
            BeginVariableScope(new VariableScope.CatchClause(node, _currentVariableScope), out Snapshot snapshot);

            var variableDeclarationVisitor = new VariableDeclarationVisitor<VariableScopeBuilder>(this,
                visitVariableIdentifier: (b, identifier) => ((VariableScope.CatchClause)b._currentVariableScope).AddParamDeclaration(identifier),
                visitRewritableExpression: (b, expression) => b.Visit(expression));

            variableDeclarationVisitor.VisitCatchClauseParam(node);

            Visit(node.Body);

            EndVariableScope(in snapshot);

            return node;
        }

        private void VisitClassCore(IClass node)
        {
            ref readonly NodeList<Decorator> decorators = ref node.Decorators;
            for (var i = 0; i < decorators.Count; i++)
            {
                Visit(decorators[i]);
            }

            if (node.SuperClass != null)
                Visit(node.SuperClass);

            Visit(node.Body);
        }

        protected override object VisitClassDeclaration(ClassDeclaration node)
        {
            if (node.Id != null)
                ((VariableScope.BlockBase)_currentVariableScope).AddClassDeclaration(node.Id);

            BeginVariableScope(new VariableScope.Class(node, _currentVariableScope), out Snapshot snapshot);

            VisitClassCore(node);

            EndVariableScope(in snapshot);

            return node;
        }

        protected override object VisitClassExpression(ClassExpression node)
        {
            // Class expression's name is not available in the enclosing scope.

            BeginVariableScope(new VariableScope.Class(node, _currentVariableScope), out Snapshot snapshot);

            VisitClassCore(node);

            EndVariableScope(in snapshot);

            return node;
        }

        protected override object VisitForInStatement(ForInStatement node)
        {
            if (!(node.Left is VariableDeclaration))
                return base.VisitForInStatement(node);

            BeginVariableScope(new VariableScope.VariableDeclaratorStatement(node, _currentVariableScope), out Snapshot snapshot);

            base.VisitForInStatement(node);

            EndVariableScope(in snapshot);

            return node;
        }

        protected override object VisitForOfStatement(ForOfStatement node)
        {
            if (!(node.Left is VariableDeclaration))
                return base.VisitForOfStatement(node);

            BeginVariableScope(new VariableScope.VariableDeclaratorStatement(node, _currentVariableScope), out Snapshot snapshot);

            base.VisitForOfStatement(node);

            EndVariableScope(in snapshot);

            return node;
        }

        protected override object VisitForStatement(ForStatement node)
        {
            if (!(node.Init is VariableDeclaration))
                return base.VisitForStatement(node);

            BeginVariableScope(new VariableScope.VariableDeclaratorStatement(node, _currentVariableScope), out Snapshot snapshot);

            base.VisitForStatement(node);

            EndVariableScope(in snapshot);

            return node;
        }

        private void VisitFunctionCore(IFunction node)
        {
            var variableDeclarationVisitor = new VariableDeclarationVisitor<VariableScopeBuilder>(this,
                visitVariableIdentifier: (b, identifier) => ((VariableScope.Function)b._currentVariableScope).AddParamDeclaration(identifier),
                visitRewritableExpression: (b, expression) => b.Visit(expression));

            variableDeclarationVisitor.VisitFunctionParams(node);

            Visit(node.Body);
        }

        protected override object VisitFunctionBody(FunctionBody node)
        {
            BeginVariableScope(new VariableScope.Block(node, _currentVariableScope), out Snapshot snapshot);

            base.VisitFunctionBody(node);

            EndVariableScope(in snapshot);

            return node;
        }

        protected override object VisitFunctionDeclaration(FunctionDeclaration node)
        {
            if (node.Id != null)
                ((VariableScope.BlockBase)_currentVariableScope).AddFunctionDeclaration(node.Id);

            BeginVariableScope(new VariableScope.Function(node, _currentVariableScope), out Snapshot snapshot);

            VisitFunctionCore(node);

            EndVariableScope(in snapshot);

            return node;
        }

        protected override object VisitFunctionExpression(FunctionExpression node)
        {
            // Function expression's name is not available in the enclosing scope.

            BeginVariableScope(new VariableScope.Function(node, _currentVariableScope), out Snapshot snapshot);

            VisitFunctionCore(node);

            EndVariableScope(in snapshot);

            return node;
        }

        protected override object VisitImportDeclaration(ImportDeclaration node)
        {
            // The parser (Acornima) makes sure that import declarations occur only at the top level.
            var topLevelScope = (VariableScope.TopLevelBlock)_currentVariableScope;

            ref readonly NodeList<ImportDeclarationSpecifier> specifiers = ref node.Specifiers;
            for (var i = 0; i < specifiers.Count; i++)
                topLevelScope.AddImportDeclaration(specifiers[i].Local);

            Visit(node.Source);

            return node;
        }

        protected override object VisitProgram(Program node)
        {
            _currentVariableScope = null;

            VariableScope.TopLevelBlock topLevelScope;

            switch (node)
            {
                case Module module:
                    topLevelScope = new VariableScope.TopLevelBlock(module);
                    break;
                case Script script:
                    topLevelScope = new VariableScope.TopLevelBlock(script);
                    break;
                default:
                    throw UnknownNodeError(node);
            }

            BeginVariableScope(topLevelScope, out Snapshot snapshot);

            base.VisitProgram(node);

            EndVariableScope(in snapshot);

            return node;
        }

        protected override object VisitSwitchStatement(SwitchStatement node)
        {
            Visit(node.Discriminant);

            BeginVariableScope(new VariableScope.Block(node, _currentVariableScope), out Snapshot snapshot);

            ref readonly NodeList<SwitchCase> cases = ref node.Cases;
            for (var i = 0; i < cases.Count; i++)
            {
                Visit(cases[i]);
            }

            EndVariableScope(in snapshot);

            return node;
        }

        protected override object VisitStaticBlock(StaticBlock node)
        {
            BeginVariableScope(new VariableScope.ClassStaticBlock(node, _currentVariableScope), out Snapshot snapshot);

            base.VisitStaticBlock(node);

            EndVariableScope(in snapshot);

            return node;
        }

        protected override object VisitVariableDeclaration(VariableDeclaration node)
        {
            var variableDeclarationVisitor = new VariableDeclarationVisitor<(VariableScopeBuilder Builder, VariableDeclaration Declaration)>((this, node),
                visitVariableIdentifier: (s, identifier) => ((VariableScope.VariableDeclaratorBase)s.Builder._currentVariableScope).AddVariableDeclaration(identifier, s.Declaration.Kind),
                visitRewritableExpression: (s, expression) => s.Builder.Visit(expression));

            ref readonly NodeList<VariableDeclarator> declarations = ref node.Declarations;
            for (var i = 0; i < declarations.Count; i++)
            {
                VariableDeclarator variableDeclarator = declarations[i];

                variableDeclarationVisitor.VisitVariableDeclaratorId(variableDeclarator);

                if (variableDeclarator.Init != null)
                    Visit(variableDeclarator.Init);
            }

            return node;
        }

        private static Exception UnknownNodeError(Node node)
        {
            return new NotSupportedException($"Nodes of type {node.Type} are not supported.");
        }
    }
}
