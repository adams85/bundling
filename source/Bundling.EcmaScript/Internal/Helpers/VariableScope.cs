using System;
using System.Collections.Generic;
using Acornima.Ast;

namespace Karambolo.AspNetCore.Bundling.EcmaScript.Internal.Helpers
{
    // https://exploringjs.com/es6/ch_variables.html
    internal abstract class VariableScope : HashSet<string>
    {
        public sealed class Function : VariableScope
        {
            public Function(ArrowFunctionExpression arrowFunctionExpression, VariableScope parentScope)
                : base(arrowFunctionExpression, parentScope, parentScope.IsStrict || arrowFunctionExpression.Body is FunctionBody { Strict: true }) { }

            public Function(FunctionDeclaration functionDeclaration, VariableScope parentScope)
                : base(functionDeclaration, parentScope, parentScope.IsStrict || functionDeclaration.Body.Strict) { }

            public Function(FunctionExpression functionExpression, VariableScope parentScope)
                : base(functionExpression, parentScope, parentScope.IsStrict || functionExpression.Body.Strict) { }

            public override VariableScope FunctionScope => this;

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

            public override VariableScope FunctionScope { get; }

            protected override void FinalizeScopeCore()
            {
                var classNode = (IClass)OriginatorNode;

                // The class's name is always available in the class's scope. (Class expressions' name is not visible in the parent scope!)
                if (classNode.Id != null)
                    Add(classNode.Id.Name);
            }
        }

        public sealed class CatchClause : VariableScope
        {
            public CatchClause(Acornima.Ast.CatchClause catchClause, VariableScope parentScope) : base(catchClause, parentScope, parentScope.IsStrict)
            {
                FunctionScope = parentScope.FunctionScope;
            }

            public override VariableScope FunctionScope { get; }

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

        public abstract class VariableDeclaratorBase : VariableScope
        {
            protected VariableDeclaratorBase(Node node, bool isStrict) : base(node, null, isStrict) { }

            protected VariableDeclaratorBase(Node node, VariableScope parentScope) : base(node, parentScope, parentScope.IsStrict) { }

            public void AddVariableDeclaration(Identifier identifier, VariableDeclarationKind declarationKind)
            {
                EnsureNotFinalized();
                Declarations.Add((identifier, ConvertToDeclarationType(declarationKind)));
            }

            private void HoistVariable(Identifier identifier)
            {
                // Declaring function-scoped variables with a name identical to some block-scoped variable declared in an enclosing block is prohibited (results in error).
                // But we don't have to deal with that as the parser (Acornima) should detect such errors.

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
                    default:
                        Add(identifier.Name);
                        break;
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

        public sealed class VariableDeclaratorStatement : VariableDeclaratorBase
        {
            public VariableDeclaratorStatement(ForInStatement forInStatement, VariableScope parentScope) : base(forInStatement, parentScope)
            {
                FunctionScope = parentScope.FunctionScope;
            }
            
            public VariableDeclaratorStatement(ForOfStatement forOfStatement, VariableScope parentScope) : base(forOfStatement, parentScope)
            {
                FunctionScope = parentScope.FunctionScope;
            }

            public VariableDeclaratorStatement(ForStatement forStatement, VariableScope parentScope) : base(forStatement, parentScope)
            {
                FunctionScope = parentScope.FunctionScope;
            }

            public override VariableScope FunctionScope { get; }
        }

        public abstract class BlockBase : VariableDeclaratorBase
        {
            protected BlockBase(Node block, bool isStrict) : base(block, isStrict) { }

            protected BlockBase(Node block, VariableScope parentScope) : base(block, parentScope) { }

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
                    default:
                        base.FinalizeDeclaration(identifier, declarationType);
                        break;
                }
            }
        }

        public sealed class TopLevelBlock : BlockBase
        {
            public TopLevelBlock(Script script) : base(script, script.Strict) { }

            public TopLevelBlock(Module module) : base(module, isStrict: true) { }

            public override VariableScope FunctionScope => this;

            public void AddImportDeclaration(Identifier identifier)
            {
                EnsureNotFinalized();
                Declarations.Add((identifier, VariableDeclarationType.Import));
            }

            protected override void FinalizeDeclaration(Identifier identifier, VariableDeclarationType declarationType)
            {
                Add(identifier.Name);
            }
        }

        public sealed class Block : BlockBase
        {
            public Block(BlockStatement blockStatement, VariableScope parentScope) : base(blockStatement, parentScope)
            {
                FunctionScope = parentScope.FunctionScope;
            }
            
            public Block(SwitchStatement switchStatement, VariableScope parentScope) : base(switchStatement, parentScope)
            {
                FunctionScope = parentScope.FunctionScope;
            }

            public override VariableScope FunctionScope { get; }
        }

        public sealed class ClassStaticBlock : BlockBase
        {
            public ClassStaticBlock(StaticBlock staticBlock, VariableScope parentScope) : base(staticBlock, parentScope) { }

            public override VariableScope FunctionScope => this;

            protected override void FinalizeDeclaration(Identifier identifier, VariableDeclarationType declarationType)
            {
                Add(identifier.Name);
            }
        }

        private static VariableDeclarationType ConvertToDeclarationType(VariableDeclarationKind declarationKind)
        {
            switch (declarationKind)
            {
                case VariableDeclarationKind.Var: return VariableDeclarationType.Var;
                case VariableDeclarationKind.Let: return VariableDeclarationType.Let;
                case VariableDeclarationKind.Const: return VariableDeclarationType.Const;
                default: throw new ArgumentOutOfRangeException(nameof(declarationKind), declarationKind, null);
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
        public abstract VariableScope FunctionScope { get; }

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
