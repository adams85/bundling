using System;
using System.Collections.Generic;
using Acornima.Ast;

namespace Karambolo.AspNetCore.Bundling.EcmaScript.Internal.Helpers
{
    internal sealed class VariableScope : HashSet<string>
    {
        private (Identifier Id, VariableKind Kind)[] _variables;

        public VariableScope(Node originatorNode,
            ReadOnlySpan<Identifier> varVariables,
            ReadOnlySpan<Identifier> lexicalVariables,
            ReadOnlySpan<Identifier> functions)
        {
            OriginatorNode = originatorNode;

            var declarationCount = varVariables.Length + lexicalVariables.Length + functions.Length;
            if (declarationCount > 0)
            {
                _variables = new (Identifier, VariableKind)[declarationCount];

                var i = 0;
                foreach (Identifier id in varVariables)
                {
                    _variables[i++] = (id, VariableKind.Var);
                }
                foreach (Identifier id in lexicalVariables)
                {
                    _variables[i++] = (id, VariableKind.Lexical);
                }
                foreach (Identifier id in functions)
                {
                    _variables[i++] = (id, VariableKind.Function);
                }
            }
            else
            {
                _variables = Array.Empty<(Identifier, VariableKind)>();
            }
        }

        internal void Initialize(VariableScope parentScope, bool isFunctionScope = false) // called by VariableScopeBuilder
        {
            if (IsInitialized)
                throw new InvalidOperationException("Variable scope has already been initialized.");

            ParentScope = parentScope;
            FunctionScope = isFunctionScope ? this : parentScope.FunctionScope;
        }

        private bool IsInitialized => FunctionScope != null;

        public Node OriginatorNode { get; }
        public IHoistingScope HoistingScopeNode
        {
            get
            {
                Node node = FunctionScope.OriginatorNode;
                return node.Type == NodeType.Program || node.Type == NodeType.StaticBlock
                    ? node.As<IHoistingScope>()
                    : node.As<IFunction>().Body as FunctionBody ?? FunctionScope.ParentScope.HoistingScopeNode;
            }
        }

        public VariableScope ParentScope { get; private set; }
        public VariableScope FunctionScope { get; private set; }

        private bool IsFinalized => _variables == null;

        internal void Finalize(Action<VariableScope> finalizer) // called by VariableScopeBuilder
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Variable scope has not been initialized yet.");

            if (IsFinalized)
                throw new InvalidOperationException("Variable scope has already been finalized.");

            finalizer(this);

            _variables = null;
        }

        private static readonly Action<VariableScope> s_addAllDeclarations = @this =>
        {
            for (int i = 0; i < @this._variables.Length; i++)
                @this.Add(@this._variables[i].Id.Name);
        };

        internal static readonly Action<VariableScope> TopLevelScopeFinalizer = s_addAllDeclarations;

        internal static readonly Action<VariableScope> FunctionBodyScopeFinalizer = s_addAllDeclarations;

        internal static readonly Action<VariableScope> ClassStaticBlockScopeFinalizer = s_addAllDeclarations;

        internal static readonly Action<VariableScope> NestedBlockScopeFinalizer = @this =>
        {
            for (int i = 0; i < @this._variables.Length; i++)
            {
                (Identifier id, VariableKind kind) = @this._variables[i];

                @this.Add(id.Name);

                // No need to hoist var variables as Acornima has already performed that.
                // However, Acornima doesn't hoist function declarations, so we need to take care of that.
                if (kind == VariableKind.Function)
                {
                    @this.HoistFunction(id.Name);
                }
            }
        };

        internal static readonly Action<VariableScope> FunctionScopeFinalizer = @this =>
        {
            IFunction functionNode = @this.OriginatorNode.As<IFunction>();

            // The function's name is always available in the function's scope. Further important remarks:
            // 1. Function expressions' name is not visible in the parent scope!
            // 2. Class/object methods are function expressions with no name and the method identifier is not visible in the method's scope
            if (functionNode.Id != null)
                @this.Add(functionNode.Id.Name);


            // 1. Functions define a special variable named 'arguments', which largely acts as a hidden parameter as it's even visible in parameter declaration:
            // (() => {
            //   function f(a, b = arguments) { return b }
            //   console.log(f(0))
            // })()
            // -> Arguments { 0: 0, … }
            // 2. Arguments object is not available in arrow functions.
            // See also: https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Strict_mode#making_eval_and_arguments_simpler
            if (!(functionNode is ArrowFunctionExpression))
                @this.Add("arguments");

            s_addAllDeclarations(@this);
        };

        internal static readonly Action<VariableScope> ClassScopeFinalizer = @this =>
        {
            IClass classNode = @this.OriginatorNode.As<IClass>();

            // The class's name is always available in the class's scope. (Class expressions' name is not visible in the parent scope!)
            if (classNode.Id != null)
                @this.Add(classNode.Id.Name);
        };

        // No need to hoist var variables as Acornima has already performed that. 
        internal static readonly Action<VariableScope> ForStatementScopeFinalizer = s_addAllDeclarations;

        internal static readonly Action<VariableScope> CatchClauseScopeFinalizer = NestedBlockScopeFinalizer;

        private void HoistFunction(string name)
        {
            // In strict mode, function declarations don't get hoisted out of the declaring block.
            if (HoistingScopeNode.Strict)
                return;

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

            VariableScope scope = this;
            while (!(scope.OriginatorNode is IHoistingScope))
            {
                VariableScope parentScope = scope.ParentScope;

                for (int i = 0; i < parentScope._variables.Length; i++)
                {
                    (Identifier otherId, VariableKind otherKind) = parentScope._variables[i];

                    if (otherId.Name == name)
                    {
                        if (parentScope.OriginatorNode is CatchClause catchClause && catchClause.Param is Identifier simpleCatchClauseParam && simpleCatchClauseParam.Name == name)
                            goto ContinueOuter;
                        else
                            return;
                    }
                }

                parentScope.Add(name);

ContinueOuter:
                scope = parentScope;
            }
        }

        public VariableScope FindIdentifier(string name)
        {
            if (!IsFinalized)
                throw new InvalidOperationException("Variable scope has not been finalized yet.");

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
