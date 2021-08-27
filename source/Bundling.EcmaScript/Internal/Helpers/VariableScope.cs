using System;
using System.Collections.Generic;
using Esprima.Ast;

namespace Karambolo.AspNetCore.Bundling.EcmaScript.Internal.Helpers
{
    // https://exploringjs.com/es6/ch_variables.html
    internal abstract class VariableScope : HashSet<string>
    {
        public abstract class FunctionBase : VariableScope
        {
            protected FunctionBase(Node originatorNode, VariableScope parentScope, bool isStrict) : base(originatorNode, parentScope, isStrict) { }

            public sealed override FunctionBase FunctionScope => this;
        }

        public sealed class Global : FunctionBase
        {
            public sealed class Placeholder : Node
            {
                public Placeholder(Program program) : base((Nodes)(int.MinValue))
                {
                    Program = program;
                }

                public Program Program { get; }

                public override NodeCollection ChildNodes => throw new NotSupportedException();
            }

            public Global(Script script) : base(new Placeholder(script), null, script.Strict) { }

            public Global(Module module) : base(new Placeholder(module), null, isStrict: true) { }
        }

        public sealed class Function : FunctionBase
        {
            // Strict mode tracking of functions is quite broken in Esprima.NET currently, so we need to look for the directive manually.
            // TODO: This workaround can be removed after https://github.com/sebastienros/esprima-dotnet/issues/179 gets resolved.
            private static bool GetIsStrict(IFunction function) =>
                function.Body is BlockStatement body &&
                body.Body.Count > 0 &&
                body.Body[0] is ExpressionStatement expressionStatement &&
                expressionStatement.Expression is Literal literal &&
                literal.StringValue == "use strict";

            public Function(ArrowFunctionExpression arrowFunctionExpression, VariableScope parentScope)
                : base(arrowFunctionExpression, parentScope, parentScope.IsStrict || GetIsStrict(arrowFunctionExpression)) { }

            public Function(FunctionDeclaration functionDeclaration, VariableScope parentScope)
                : base(functionDeclaration, parentScope, parentScope.IsStrict || GetIsStrict(functionDeclaration)) { }

            public Function(FunctionExpression functionExpression, VariableScope parentScope)
                : base(functionExpression, parentScope, parentScope.IsStrict || GetIsStrict(functionExpression)) { }

            public void AddParamDeclaration(Identifier identifier)
            {
                EnsureNotFinalized();
                Declarations.Add((identifier, VariableDeclarationType.FunctionParam));
            }

            protected override void FinalizeScopeCore()
            {
                var functionNode = (IFunction)OriginatorNode;

                // The function's name is always available in the function's scope. Further important remarks:
                // 1. Function expressions' name is not visible in the parent scope!
                // 2. Class/object methods are function expressions with no name and the method identifier is not visible in the method's scope.
                if (functionNode.Id != null)
                    Add(functionNode.Id.Name);

                // 1. Functions define a special variable named 'arguments', which largely acts as a hidden parameter as it's even visible in parameter declaration:
                // (() => {
                //   function f(a, b = arguments) { return b }
                //   console.log(f(0))
                // })()
                // -> Arguments { 0: 0, … }
                // 2. Arguments object is not available in arrow functions.
                // See also: https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Strict_mode#making_eval_and_arguments_simpler
                if (!(functionNode is ArrowFunctionExpression))
                    Add("arguments");

                for (int i = 0, n = Declarations.Count; i < n; i++)
                    Add(Declarations[i].Identifier.Name);
            }
        }

        public sealed class Class : VariableScope
        {
            // Classes are always in strict mode.
            // https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Strict_mode#strict_mode_for_classes

            public Class(ClassDeclaration classDeclaration, VariableScope parentScope) : base(classDeclaration, parentScope, isStrict: true)
            {
                FunctionScope = parentScope.FunctionScope;
            }

            public Class(ClassExpression classExpression, VariableScope parentScope) : base(classExpression, parentScope, isStrict: true)
            {
                FunctionScope = parentScope.FunctionScope;
            }

            public override FunctionBase FunctionScope { get; }

            protected override void FinalizeScopeCore()
            {
                Identifier classIdentifier =
                    OriginatorNode is ClassDeclaration classDeclaration ? classDeclaration.Id :
                    OriginatorNode is ClassExpression classExpression ? classExpression.Id :
                    null;

                // The class's name is always available in the class's scope. (Class expressions' name is not visible in the parent scope!)
                if (classIdentifier != null)
                    Add(classIdentifier.Name);
            }
        }

        public sealed class CatchClause : VariableScope
        {
            public CatchClause(Esprima.Ast.CatchClause catchClause, VariableScope parentScope) : base(catchClause, parentScope, parentScope.IsStrict)
            {
                FunctionScope = parentScope.FunctionScope;
            }

            public override FunctionBase FunctionScope { get; }

            public void AddParamDeclaration(Identifier identifier)
            {
                EnsureNotFinalized();
                Declarations.Add((identifier, VariableDeclarationType.CatchClauseParam));
            }

            protected override void FinalizeScopeCore()
            {
                for (int i = 0, n = Declarations.Count; i < n; i++)
                    Add(Declarations[i].Identifier.Name);
            }
        }

        public abstract class StatementBase : VariableScope
        {
            protected StatementBase(Statement statement, VariableScope parentScope) : base(statement, parentScope, parentScope.IsStrict)
            {
                FunctionScope = parentScope.FunctionScope;
            }

            public sealed override FunctionBase FunctionScope { get; }

            public void AddVariableDeclaration(Identifier identifier, VariableDeclarationKind declarationKind)
            {
                EnsureNotFinalized();
                Declarations.Add((identifier, ConvertToDeclarationType(declarationKind)));
            }

            private void HoistVariable(Identifier identifier)
            {
                // Declaring function-scoped variables with a name identical to some block-scoped variable declared in an enclosing block is prohibited (results in error).
                // TODO: It'd be nice to track down and report these errors to the consumer but the logic behind the redeclarations detection seems pretty complex and messy...

                VariableScope scope;
                for (scope = this; scope.ParentScope != FunctionScope; scope = scope.ParentScope) { }

                scope.Add(identifier.Name);
            }

            protected virtual void FinalizeDeclaration(Identifier identifier, VariableDeclarationType declarationType)
            {
                switch (declarationType)
                {
                    case VariableDeclarationType.Var:
                        HoistVariable(identifier);
                        break;
                    case VariableDeclarationType.Let:
                    case VariableDeclarationType.Const:
                        Add(identifier.Name);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(declarationType));
                }
            }

            protected sealed override void FinalizeScopeCore()
            {
                for (int i = 0, n = Declarations.Count; i < n; i++)
                {
                    (Identifier identifier, VariableDeclarationType declarationType) = Declarations[i];
                    FinalizeDeclaration(identifier, declarationType);
                }
            }
        }

        public sealed class DeclaratorStatement : StatementBase
        {
            public DeclaratorStatement(ForInStatement forInStatement, VariableScope parentScope) : base(forInStatement, parentScope) { }
            
            public DeclaratorStatement(ForOfStatement forOfStatement, VariableScope parentScope) : base(forOfStatement, parentScope) { }

            public DeclaratorStatement(ForStatement forStatement, VariableScope parentScope) : base(forStatement, parentScope) { }
        }

        public abstract class BlockBase : StatementBase
        {
            protected BlockBase(Statement statement, VariableScope parentScope) : base(statement, parentScope) { }

            public void AddClassDeclaration(Identifier identifier)
            {
                EnsureNotFinalized();
                Declarations.Add((identifier, VariableDeclarationType.Class));
            }

            public void AddFunctionDeclaration(Identifier identifier)
            {
                EnsureNotFinalized();
                Declarations.Add((identifier, VariableDeclarationType.Function));
            }

            private void HoistFunction(Identifier identifier)
            {
                // In strict mode, function declarations don't get hoisted out of the declaring block.
                if (IsStrict)
                {
                    Add(identifier.Name);
                    return;
                }

                // As opposed to var variables, declaring functions with a name identical to some block-scoped variable declared in an enclosing block is not prohibited,
                // but such variables prevents further hoisting!!! 
                // (() => {
                //   console.log(x);
                //   { 
                //     const x = 0;
                //     { 
                //       function x() {} 
                //     }
                //   }
                // })()
                // -> ReferenceError: x is not defined

                // Catch clause params don't prevent function hoisting. To be more precise, identical names in the immediate block result in redeclaration error,
                // but in a nested block it's ok (tested in Firefox 90, Edge Chromium 92).
                // (() => {
                //   console.log(x);
                //   { 
                //     try { throw 0 }
                //     catch(x) {
                //       // function x() {} // -> SyntaxError: redeclaration of catch parameter x !!!
                //       { function x() {} } // no error and hoisted to the function scope... WTF???
                //     }
                //   }
                //   console.log(x);
                // })()
                // -> undefined
                const VariableDeclarationType declarationTypesPreventingHoisting = VariableDeclarationType.Let | VariableDeclarationType.Const | VariableDeclarationType.Class;

                VariableScope scope;
                for (scope = this; scope.ParentScope != FunctionScope; scope = scope.ParentScope)
                    for (int i = 0, n = scope.ParentScope.Declarations.Count; i < n; i++)
                    {
                        (Identifier otherIdentifier, VariableDeclarationType otherDeclarationType) = scope.ParentScope.Declarations[i];
                        if ((declarationTypesPreventingHoisting & otherDeclarationType) != 0 && otherIdentifier.Name == identifier.Name)
                            break;
                    }

                scope.Add(identifier.Name);
            }

            protected override void FinalizeDeclaration(Identifier identifier, VariableDeclarationType declarationType)
            {
                switch (declarationType)
                {
                    case VariableDeclarationType.Function:
                        HoistFunction(identifier);
                        break;
                    case VariableDeclarationType.Class:
                        Add(identifier.Name);
                        break;
                    default:
                        base.FinalizeDeclaration(identifier, declarationType);
                        break;
                }
            }
        }

        public sealed class GlobalBlock: BlockBase
        {
            public GlobalBlock(Program program, Global globalScope) : base(program, globalScope) { }

            public void AddImportDeclaration(Identifier identifier)
            {
                EnsureNotFinalized();
                Declarations.Add((identifier, VariableDeclarationType.Import));
            }

            protected override void FinalizeDeclaration(Identifier identifier, VariableDeclarationType declarationType)
            {
                switch (declarationType)
                {
                    case VariableDeclarationType.Import:
                        Add(identifier.Name);
                        break;
                    default:
                        base.FinalizeDeclaration(identifier, declarationType);
                        break;
                }
            }
        }

        public sealed class Block : BlockBase
        {
            public Block(BlockStatement blockStatement, VariableScope parentScope) : base(blockStatement, parentScope) { }
        }

        private static VariableDeclarationType ConvertToDeclarationType(VariableDeclarationKind declarationKind)
        {
            switch (declarationKind)
            {
                case VariableDeclarationKind.Var: return VariableDeclarationType.Var;
                case VariableDeclarationKind.Let: return VariableDeclarationType.Let;
                case VariableDeclarationKind.Const: return VariableDeclarationType.Const;
                default: throw new ArgumentOutOfRangeException(nameof(declarationKind));
            }
        }

        protected VariableScope(Node originatorNode, VariableScope parentScope, bool isStrict)
        {
            ParentScope = parentScope;
            OriginatorNode = originatorNode;
            IsStrict = isStrict;

            Declarations = new List<(Identifier, VariableDeclarationType)>();
        }

        public VariableScope ParentScope { get; }
        public abstract FunctionBase FunctionScope { get; }

        public Node OriginatorNode { get; }

        public bool IsStrict { get; }

        protected bool IsFinalized => Declarations == null;

        protected List<(Identifier Identifier, VariableDeclarationType Kind)> Declarations { get; private set; }

        protected void EnsureNotFinalized()
        {
            if (IsFinalized)
                throw new InvalidOperationException("Variable scope has already been finalized.");
        }

        protected virtual void FinalizeScopeCore() { }

        public void FinalizeScope()
        {
            EnsureNotFinalized();
            FinalizeScopeCore();
            Declarations = null;
        }

        protected void EnsureFinalized()
        {
            if (!IsFinalized)
                throw new InvalidOperationException("Variable scope has not been finalized yet.");
        }

        public VariableScope FindIdentifier(string name)
        {
            EnsureFinalized();

            VariableScope scope = this;
            do
            {
                if (scope.Contains(name))
                    return scope;
            }
            while ((scope = scope.ParentScope) != null);

            return null;
        }
    }
}
