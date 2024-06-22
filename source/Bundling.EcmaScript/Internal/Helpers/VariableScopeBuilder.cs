using System;
using Acornima;
using Acornima.Ast;

namespace Karambolo.AspNetCore.Bundling.EcmaScript.Internal.Helpers
{
    internal class VariableScopeBuilder : AstVisitor
    {
        public static readonly OnNodeHandler OnNodeHandler = (node, context) =>
        {
            if (!context.HasScope)
                return;

            ReadOnlySpan<Identifier> varVariables = context.Scope.VarVariables;
            ReadOnlySpan<Identifier> lexicalVariables = context.Scope.LexicalVariables;
            ReadOnlySpan<Identifier> functions = context.Scope.Functions;

            switch (node.Type)
            {
                case NodeType.CatchClause:
                    NestedBlockStatement body = node.As<CatchClause>().Body;
                    body.UserData = new VariableScope(body, varVariables, lexicalVariables.Slice(context.Scope.LexicalParamCount), functions);
                    lexicalVariables = lexicalVariables.Slice(0, context.Scope.LexicalParamCount);
                    break;

                case NodeType.ArrowFunctionExpression:
                case NodeType.FunctionDeclaration:
                case NodeType.FunctionExpression:
                    if (node.As<IFunction>().Body is FunctionBody functionBody)
                    {
                        functionBody.UserData = new VariableScope(functionBody, varVariables.Slice(context.Scope.VarParamCount), lexicalVariables, functions);
                    }
                    varVariables = varVariables.Slice(0, context.Scope.VarParamCount);
                    lexicalVariables = functions = default;
                    break;
            }

            node.UserData = new VariableScope(node, varVariables, lexicalVariables, functions);
        };

        private VariableScope _currentVariableScope;
        protected VariableScope CurrentVariableScope => _currentVariableScope;

        protected override void Reset()
        {
            _currentVariableScope = null;
        }

        protected void BeginVariableScope(VariableScope variableScope, bool isFunctionScope = false)
        {
            variableScope.Initialize(_currentVariableScope, isFunctionScope);
            _currentVariableScope = variableScope;
        }

        protected void EndVariableScope(Action<VariableScope> finalizer)
        {
            VariableScope variableScope = _currentVariableScope;
            _currentVariableScope = _currentVariableScope.ParentScope;
            variableScope.Finalize(finalizer);
        }

        protected override object VisitArrowFunctionExpression(ArrowFunctionExpression node)
        {
            BeginVariableScope((VariableScope)node.UserData, isFunctionScope: true);

            base.VisitArrowFunctionExpression(node);

            EndVariableScope(VariableScope.FunctionScopeFinalizer);

            return node;
        }

        protected override object VisitBlockStatement(BlockStatement node)
        {
            BeginVariableScope((VariableScope)node.UserData);

            base.VisitBlockStatement(node);

            EndVariableScope(VariableScope.NestedBlockScopeFinalizer);

            return node;
        }

        protected override object VisitCatchClause(CatchClause node)
        {
            BeginVariableScope((VariableScope)node.UserData);

            base.VisitCatchClause(node);

            EndVariableScope(VariableScope.CatchClauseScopeFinalizer);

            return node;
        }

        protected override object VisitClassDeclaration(ClassDeclaration node)
        {
            BeginVariableScope((VariableScope)node.UserData);

            base.VisitClassDeclaration(node);

            EndVariableScope(VariableScope.ClassScopeFinalizer);

            return node;
        }

        protected override object VisitClassExpression(ClassExpression node)
        {
            BeginVariableScope((VariableScope)node.UserData);

            base.VisitClassExpression(node);

            EndVariableScope(VariableScope.ClassScopeFinalizer);

            return node;
        }

        protected override object VisitForInStatement(ForInStatement node)
        {
            BeginVariableScope((VariableScope)node.UserData);

            base.VisitForInStatement(node);

            EndVariableScope(VariableScope.ForStatementScopeFinalizer);

            return node;
        }

        protected override object VisitForOfStatement(ForOfStatement node)
        {
            BeginVariableScope((VariableScope)node.UserData);

            base.VisitForOfStatement(node);

            EndVariableScope(VariableScope.ForStatementScopeFinalizer);

            return node;
        }

        protected override object VisitForStatement(ForStatement node)
        {
            BeginVariableScope((VariableScope)node.UserData);

            base.VisitForStatement(node);

            EndVariableScope(VariableScope.ForStatementScopeFinalizer);

            return node;
        }

        protected override object VisitFunctionBody(FunctionBody node)
        {
            BeginVariableScope((VariableScope)node.UserData);

            base.VisitFunctionBody(node);

            EndVariableScope(VariableScope.FunctionBodyScopeFinalizer);

            return node;
        }

        protected override object VisitFunctionDeclaration(FunctionDeclaration node)
        {
            BeginVariableScope((VariableScope)node.UserData, isFunctionScope: true);

            base.VisitFunctionDeclaration(node);

            EndVariableScope(VariableScope.FunctionScopeFinalizer);

            return node;
        }

        protected override object VisitFunctionExpression(FunctionExpression node)
        {
            BeginVariableScope((VariableScope)node.UserData, isFunctionScope: true);

            base.VisitFunctionExpression(node);

            EndVariableScope(VariableScope.FunctionScopeFinalizer);

            return node;
        }

        protected override object VisitProgram(Program node)
        {
            Reset();

            BeginVariableScope((VariableScope)node.UserData, isFunctionScope: true);

            base.VisitProgram(node);

            EndVariableScope(VariableScope.TopLevelScopeFinalizer);

            return node;
        }

        protected override object VisitSwitchStatement(SwitchStatement node)
        {
            // The discriminant expression must be visited outside the scope assigned to the switch statement
            // as the variables declared in the statement body must not be visible from the discriminator.
            Visit(node.Discriminant);

            BeginVariableScope((VariableScope)node.UserData);

            ref readonly NodeList<SwitchCase> cases = ref node.Cases;
            for (var i = 0; i < cases.Count; i++)
            {
                Visit(cases[i]);
            }

            EndVariableScope(VariableScope.NestedBlockScopeFinalizer);

            return node;
        }

        protected override object VisitStaticBlock(StaticBlock node)
        {
            BeginVariableScope((VariableScope)node.UserData, isFunctionScope: true);

            base.VisitStaticBlock(node);

            EndVariableScope(VariableScope.ClassStaticBlockScopeFinalizer);

            return node;
        }

        private static Exception UnknownNodeError(Node node)
        {
            return new NotSupportedException($"Nodes of type {node.Type} are not supported.");
        }
    }
}
