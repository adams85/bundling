using System.Collections.Generic;
using System.Linq;
using Acornima;
using Acornima.Ast;
using Xunit;

namespace Karambolo.AspNetCore.Bundling.EcmaScript.Internal.Helpers
{
    public class VariableScopeBuilderTest
    {
        private static VariableScope GetScope(Node node, bool assertOriginator = true)
        {
            var scope = (VariableScope)node.UserData;
            if (scope != null && assertOriginator)
            {
                Assert.Same(scope.OriginatorNode, node);
            }
            return scope;
        }

        [Fact]
        public void FindIdentifer_TopLevelScope()
        {
            var moduleContent =
@"import defaultImport, * as namespaceImport from './foo.js';
import { import1, x as aliasImport } from './foo.js';

var globalVar1, globalVar2 = 0;
const [globalConst1, globalConst2] = [0, 1];
let {globalLet1, b: [globalLet2, globalLet3] = [globalConst1 + 3, globalConst2 + 3]} = { globalLet1: 2 };

class GlobalClass1 {
    method(methodParam, aliasImport, globalVar2, globalConst2, globalLet2, globalFunc2, GlobalClass2) {
        var methodLocalVar = 0;
        let methodLocalLet = 1;
    }
}

class GlobalClass2 { }

function globalFunc1(funcParam, aliasImport, globalVar2, globalConst2, globalLet2, globalFunc2, GlobalClass2) { 
  var funcLocalVar = 0;
  let funcLocalLet = 1;

  class FuncLocalClass { }
  
  function funcLocalFunc() { }
}

function globalFunc2() { }

function globalFunc3() { }
";

            Module moduleAst = new Parser(ModuleBundler.CreateParserOptions()).ParseModule(moduleContent);

            var scopeBuilder = new VariableScopeBuilder();
            scopeBuilder.Visit(moduleAst);

            var topLevelScope = GetScope(moduleAst);

            Assert.Same(topLevelScope, topLevelScope.FindIdentifier("defaultImport"));
            Assert.Same(topLevelScope, topLevelScope.FindIdentifier("namespaceImport"));
            Assert.Same(topLevelScope, topLevelScope.FindIdentifier("import1"));
            Assert.Same(topLevelScope, topLevelScope.FindIdentifier("aliasImport"));
            Assert.Same(topLevelScope, topLevelScope.FindIdentifier("globalVar1"));
            Assert.Same(topLevelScope, topLevelScope.FindIdentifier("globalVar2"));
            Assert.Same(topLevelScope, topLevelScope.FindIdentifier("globalConst1"));
            Assert.Same(topLevelScope, topLevelScope.FindIdentifier("globalConst2"));
            Assert.Same(topLevelScope, topLevelScope.FindIdentifier("globalLet1"));
            Assert.Same(topLevelScope, topLevelScope.FindIdentifier("globalLet2"));
            Assert.Same(topLevelScope, topLevelScope.FindIdentifier("globalLet3"));
            Assert.Same(topLevelScope, topLevelScope.FindIdentifier("GlobalClass1"));
            Assert.Null(topLevelScope.FindIdentifier("method"));
            Assert.Null(topLevelScope.FindIdentifier("methodParam"));
            Assert.Null(topLevelScope.FindIdentifier("methodLocalVar"));
            Assert.Null(topLevelScope.FindIdentifier("methodLocalLet"));
            Assert.Same(topLevelScope, topLevelScope.FindIdentifier("GlobalClass2"));
            Assert.Same(topLevelScope, topLevelScope.FindIdentifier("globalFunc1"));
            Assert.Null(topLevelScope.FindIdentifier("funcParam"));
            Assert.Null(topLevelScope.FindIdentifier("funcLocalVar"));
            Assert.Null(topLevelScope.FindIdentifier("funcLocalLet"));
            Assert.Null(topLevelScope.FindIdentifier("FuncLocalClass"));
            Assert.Null(topLevelScope.FindIdentifier("funcLocalFunc"));
            Assert.Same(topLevelScope, topLevelScope.FindIdentifier("globalFunc2"));
            Assert.Same(topLevelScope, topLevelScope.FindIdentifier("globalFunc3"));
            Assert.Null(topLevelScope.FindIdentifier("arguments"));
        }

        [Fact]
        public void FindIdentifer_ClassMethodScope()
        {
            var moduleContent =
@"import defaultImport, * as namespaceImport from './foo.js';
import { import1, x as aliasImport } from './foo.js';

var globalVar1, globalVar2 = 0;
const [globalConst1, globalConst2] = [0, 1];
let {globalLet1, b: [globalLet2, globalLet3] = [globalConst1 + 3, globalConst2 + 3]} = { globalLet1: 2 };

class GlobalClass1 {
    method(methodParam, aliasImport, globalVar2, globalConst2, globalLet2, globalFunc2, GlobalClass2) {
        var methodLocalVar = 0;
        let methodLocalLet = 1;
    }
}

class GlobalClass2 { }

function globalFunc1(funcParam, aliasImport, globalVar2, globalConst2, globalLet2, globalFunc2, GlobalClass2) { 
  var funcLocalVar = 0;
  let funcLocalLet = 1;

  class FuncLocalClass { }
  
  function funcLocalFunc() { }
}

function globalFunc2() { }

function globalFunc3() { }
";

            Module moduleAst = new Parser(ModuleBundler.CreateParserOptions()).ParseModule(moduleContent);

            var scopeBuilder = new VariableScopeBuilder();
            scopeBuilder.Visit(moduleAst);

            VariableScope topLevelScope = GetScope(moduleAst);

            ClassDeclaration classDeclaration = moduleAst.Body
                .OfType<ClassDeclaration>().Single(d => d.Id?.Name == "GlobalClass1");

            VariableScope classScope = GetScope(classDeclaration);
            Assert.Same(topLevelScope, classScope.ParentScope);

            FunctionExpression functionExpression = classDeclaration.Body.Body
                .OfType<MethodDefinition>().Single(d => d.Key is Identifier id && id.Name == "method").Value
                .As<FunctionExpression>();

            VariableScope methodFunctionScope = GetScope(functionExpression);
            Assert.Same(classScope, methodFunctionScope.ParentScope);

            FunctionBody functionBody = functionExpression.Body;

            VariableScope methodBlockScope = GetScope(functionBody);
            Assert.Same(methodFunctionScope, methodBlockScope.ParentScope);

            Assert.Same(topLevelScope, methodBlockScope.FindIdentifier("defaultImport"));
            Assert.Same(topLevelScope, methodBlockScope.FindIdentifier("namespaceImport"));
            Assert.Same(topLevelScope, methodBlockScope.FindIdentifier("import1"));
            Assert.Same(methodFunctionScope, methodBlockScope.FindIdentifier("aliasImport"));
            Assert.Same(topLevelScope, methodBlockScope.FindIdentifier("globalVar1"));
            Assert.Same(methodFunctionScope, methodBlockScope.FindIdentifier("globalVar2"));
            Assert.Same(topLevelScope, methodBlockScope.FindIdentifier("globalConst1"));
            Assert.Same(methodFunctionScope, methodBlockScope.FindIdentifier("globalConst2"));
            Assert.Same(topLevelScope, methodBlockScope.FindIdentifier("globalLet1"));
            Assert.Same(methodFunctionScope, methodBlockScope.FindIdentifier("globalLet2"));
            Assert.Same(topLevelScope, methodBlockScope.FindIdentifier("globalLet3"));
            Assert.Same(classScope, methodBlockScope.FindIdentifier("GlobalClass1"));
            Assert.Null(methodBlockScope.FindIdentifier("method"));
            Assert.Same(methodFunctionScope, methodBlockScope.FindIdentifier("methodParam"));
            Assert.Same(methodBlockScope, methodBlockScope.FindIdentifier("methodLocalVar"));
            Assert.Same(methodBlockScope, methodBlockScope.FindIdentifier("methodLocalLet"));
            Assert.Same(methodFunctionScope, methodBlockScope.FindIdentifier("GlobalClass2"));
            Assert.Same(topLevelScope, methodBlockScope.FindIdentifier("globalFunc1"));
            Assert.Null(methodBlockScope.FindIdentifier("funcParam"));
            Assert.Null(methodBlockScope.FindIdentifier("funcLocalVar"));
            Assert.Null(methodBlockScope.FindIdentifier("funcLocalLet"));
            Assert.Null(methodBlockScope.FindIdentifier("FuncLocalClass"));
            Assert.Null(methodBlockScope.FindIdentifier("funcLocalFunc"));
            Assert.Same(methodFunctionScope, methodBlockScope.FindIdentifier("globalFunc2"));
            Assert.Same(topLevelScope, methodBlockScope.FindIdentifier("globalFunc3"));
            Assert.Same(methodFunctionScope, methodBlockScope.FindIdentifier("arguments"));
        }

        [Fact]
        public void FindIdentifer_ClassStaticBlockScope()
        {
            var moduleContent =
@"import defaultImport, * as namespaceImport from './foo.js';
import { import1, x as aliasImport } from './foo.js';

var globalVar1, globalVar2 = 0;
const [globalConst1, globalConst2] = [0, 1];
let {globalLet1, b: [globalLet2, globalLet3] = [globalConst1 + 3, globalConst2 + 3]} = { globalLet1: 2 };

class GlobalClass1 {
    static {
        var methodParam, aliasImport, globalVar2, globalConst2, globalLet2, globalFunc2, GlobalClass2;
        var methodLocalVar = 0;
        let methodLocalLet = 1;
    }
}

class GlobalClass2 { }

function globalFunc1(funcParam, aliasImport, globalVar2, globalConst2, globalLet2, globalFunc2, GlobalClass2) { 
  var funcLocalVar = 0;
  let funcLocalLet = 1;

  class FuncLocalClass { }
  
  function funcLocalFunc() { }
}

function globalFunc2() { }

function globalFunc3() { }
";

            Module moduleAst = new Parser(ModuleBundler.CreateParserOptions()).ParseModule(moduleContent);

            var scopeBuilder = new VariableScopeBuilder();
            scopeBuilder.Visit(moduleAst);

            VariableScope topLevelScope = GetScope(moduleAst);

            ClassDeclaration classDeclaration = moduleAst.Body
                .OfType<ClassDeclaration>().Single(d => d.Id?.Name == "GlobalClass1");

            VariableScope classScope = GetScope(classDeclaration);
            Assert.Same(topLevelScope, classScope.ParentScope);

            StaticBlock staticBlock = classDeclaration.Body.Body
                .OfType<StaticBlock>().Single()
                .As<StaticBlock>();

            VariableScope staticBlockScope = GetScope(staticBlock);
            Assert.Same(classScope, staticBlockScope.ParentScope);

            Assert.Same(topLevelScope, staticBlockScope.FindIdentifier("defaultImport"));
            Assert.Same(topLevelScope, staticBlockScope.FindIdentifier("namespaceImport"));
            Assert.Same(topLevelScope, staticBlockScope.FindIdentifier("import1"));
            Assert.Same(staticBlockScope, staticBlockScope.FindIdentifier("aliasImport"));
            Assert.Same(topLevelScope, staticBlockScope.FindIdentifier("globalVar1"));
            Assert.Same(staticBlockScope, staticBlockScope.FindIdentifier("globalVar2"));
            Assert.Same(topLevelScope, staticBlockScope.FindIdentifier("globalConst1"));
            Assert.Same(staticBlockScope, staticBlockScope.FindIdentifier("globalConst2"));
            Assert.Same(topLevelScope, staticBlockScope.FindIdentifier("globalLet1"));
            Assert.Same(staticBlockScope, staticBlockScope.FindIdentifier("globalLet2"));
            Assert.Same(topLevelScope, staticBlockScope.FindIdentifier("globalLet3"));
            Assert.Same(classScope, staticBlockScope.FindIdentifier("GlobalClass1"));
            Assert.Null(staticBlockScope.FindIdentifier("method"));
            Assert.Same(staticBlockScope, staticBlockScope.FindIdentifier("methodParam"));
            Assert.Same(staticBlockScope, staticBlockScope.FindIdentifier("methodLocalVar"));
            Assert.Same(staticBlockScope, staticBlockScope.FindIdentifier("methodLocalLet"));
            Assert.Same(staticBlockScope, staticBlockScope.FindIdentifier("GlobalClass2"));
            Assert.Same(topLevelScope, staticBlockScope.FindIdentifier("globalFunc1"));
            Assert.Null(staticBlockScope.FindIdentifier("funcParam"));
            Assert.Null(staticBlockScope.FindIdentifier("funcLocalVar"));
            Assert.Null(staticBlockScope.FindIdentifier("funcLocalLet"));
            Assert.Null(staticBlockScope.FindIdentifier("FuncLocalClass"));
            Assert.Null(staticBlockScope.FindIdentifier("funcLocalFunc"));
            Assert.Same(staticBlockScope, staticBlockScope.FindIdentifier("globalFunc2"));
            Assert.Same(topLevelScope, staticBlockScope.FindIdentifier("globalFunc3"));
            Assert.Null(staticBlockScope.FindIdentifier("arguments"));
        }

        [Fact]
        public void FindIdentifer_FunctionScope()
        {
            var moduleContent =
@"import defaultImport, * as namespaceImport from './foo.js';
import { import1, x as aliasImport } from './foo.js';

var globalVar1, globalVar2 = 0;
const [globalConst1, globalConst2] = [0, 1];
let {globalLet1, b: [globalLet2, globalLet3] = [globalConst1 + 3, globalConst2 + 3]} = { globalLet1: 2 };

class GlobalClass1 {
    method(methodParam, aliasImport, globalVar2, globalConst2, globalLet2, globalFunc2, GlobalClass2) {
        var methodLocalVar = 0;
        let methodLocalLet = 1;
    }
}

class GlobalClass2 { }

function globalFunc1(funcParam, aliasImport, globalVar2, globalConst2, globalLet2, globalFunc2, GlobalClass2) { 
  var funcLocalVar = 0;
  let funcLocalLet = 1;

  class FuncLocalClass { }
  
  function funcLocalFunc() { }
}

function globalFunc2() { }

function globalFunc3() { }
";

            Module moduleAst = new Parser(ModuleBundler.CreateParserOptions()).ParseModule(moduleContent);

            var scopeBuilder = new VariableScopeBuilder();
            scopeBuilder.Visit(moduleAst);

            VariableScope topLevelScope = GetScope(moduleAst);
            
            FunctionDeclaration functionDeclaration = moduleAst.Body
                .OfType<FunctionDeclaration>().Single(d => d.Id?.Name == "globalFunc1");

            VariableScope functionScope = GetScope(functionDeclaration);
            Assert.Same(topLevelScope, functionScope.ParentScope);

            FunctionBody functionBody = functionDeclaration.Body;

            VariableScope functionBlockScope = GetScope(functionBody);
            Assert.Same(functionScope, functionBlockScope.ParentScope);

            Assert.Same(topLevelScope, functionBlockScope.FindIdentifier("defaultImport"));
            Assert.Same(topLevelScope, functionBlockScope.FindIdentifier("namespaceImport"));
            Assert.Same(topLevelScope, functionBlockScope.FindIdentifier("import1"));
            Assert.Same(functionScope, functionBlockScope.FindIdentifier("aliasImport"));
            Assert.Same(topLevelScope, functionBlockScope.FindIdentifier("globalVar1"));
            Assert.Same(functionScope, functionBlockScope.FindIdentifier("globalVar2"));
            Assert.Same(topLevelScope, functionBlockScope.FindIdentifier("globalConst1"));
            Assert.Same(functionScope, functionBlockScope.FindIdentifier("globalConst2"));
            Assert.Same(topLevelScope, functionBlockScope.FindIdentifier("globalLet1"));
            Assert.Same(functionScope, functionBlockScope.FindIdentifier("globalLet2"));
            Assert.Same(topLevelScope, functionBlockScope.FindIdentifier("globalLet3"));
            Assert.Same(topLevelScope, functionBlockScope.FindIdentifier("GlobalClass1"));
            Assert.Null(functionBlockScope.FindIdentifier("method"));
            Assert.Null(functionBlockScope.FindIdentifier("methodParam"));
            Assert.Null(functionBlockScope.FindIdentifier("methodLocalVar"));
            Assert.Null(functionBlockScope.FindIdentifier("methodLocalLet"));
            Assert.Same(functionScope, functionBlockScope.FindIdentifier("GlobalClass2"));
            Assert.Same(functionScope, functionBlockScope.FindIdentifier("globalFunc1"));
            Assert.Same(functionScope, functionBlockScope.FindIdentifier("funcParam"));
            Assert.Same(functionBlockScope, functionBlockScope.FindIdentifier("funcLocalVar"));
            Assert.Same(functionBlockScope, functionBlockScope.FindIdentifier("funcLocalLet"));
            Assert.Same(functionBlockScope, functionBlockScope.FindIdentifier("FuncLocalClass"));
            Assert.Same(functionBlockScope, functionBlockScope.FindIdentifier("funcLocalFunc"));
            Assert.Same(functionScope, functionBlockScope.FindIdentifier("globalFunc2"));
            Assert.Same(topLevelScope, functionBlockScope.FindIdentifier("globalFunc3"));
            Assert.Same(functionScope, functionBlockScope.FindIdentifier("arguments"));
        }

        [Fact]
        public void FindIdentifer_VariableDeclaratorStatementScope()
        {
            var moduleContent =
@"(() => {
  for (var i = 0, n = 1; i < n; i++) {
    const x = i;
    console.log(x);
  }

  for (let x in {[0]: 0}) {
    const x = 1;
    console.log(x);
    var y = -x;
  }

  for (var { y: a, b = y + 4 } of [{ y: y + 3 }])
    var y = (function f() { console.log(a); console.log(b); return a + 2; })();
  
  console.log(y);
})()
";

            Module moduleAst = new Parser(ModuleBundler.CreateParserOptions()).ParseModule(moduleContent);

            var scopeBuilder = new VariableScopeBuilder();
            scopeBuilder.Visit(moduleAst);

            VariableScope topLevelScope = GetScope(moduleAst);

            ArrowFunctionExpression functionExpression = moduleAst.Body.Single()
                .As<NonSpecialExpressionStatement>().Expression
                .As<CallExpression>().Callee
                .As<ArrowFunctionExpression>();

            VariableScope wrapperFunctionScope = GetScope(functionExpression);
            Assert.Same(topLevelScope, wrapperFunctionScope.ParentScope);

            FunctionBody wrapperFunctionBody = functionExpression.Body.As<FunctionBody>();

            VariableScope wrapperFunctionBlockScope = GetScope(wrapperFunctionBody);
            Assert.Same(wrapperFunctionScope, wrapperFunctionBlockScope.ParentScope);

            Assert.Same(wrapperFunctionBlockScope, wrapperFunctionBlockScope.FindIdentifier("i"));
            Assert.Same(wrapperFunctionBlockScope, wrapperFunctionBlockScope.FindIdentifier("n"));
            Assert.Null(wrapperFunctionBlockScope.FindIdentifier("x"));
            Assert.Same(wrapperFunctionBlockScope, wrapperFunctionBlockScope.FindIdentifier("y"));
            Assert.Null(wrapperFunctionBlockScope.FindIdentifier("f"));
            Assert.Same(wrapperFunctionBlockScope, wrapperFunctionBlockScope.FindIdentifier("a"));
            Assert.Same(wrapperFunctionBlockScope, wrapperFunctionBlockScope.FindIdentifier("b"));

            // for

            ForStatement forStatement = wrapperFunctionBody.Body.OfType<ForStatement>().Single();

            VariableScope forScope = GetScope(forStatement);
            Assert.Same(wrapperFunctionBlockScope, forScope.ParentScope);

            Assert.Same(forScope, forScope.FindIdentifier("i"));
            Assert.Same(forScope, forScope.FindIdentifier("n"));
            Assert.Null(forScope.FindIdentifier("x"));
            Assert.Same(wrapperFunctionBlockScope, wrapperFunctionBlockScope.FindIdentifier("y"));
            Assert.Null(forScope.FindIdentifier("f"));
            Assert.Same(wrapperFunctionBlockScope, forScope.FindIdentifier("a"));
            Assert.Same(wrapperFunctionBlockScope, forScope.FindIdentifier("b"));

            NestedBlockStatement blockStatement = forStatement.Body.As<NestedBlockStatement>();

            VariableScope forBlockScope = GetScope(blockStatement);
            Assert.Same(forScope, forBlockScope.ParentScope);

            Assert.Same(forScope, forBlockScope.FindIdentifier("i"));
            Assert.Same(forScope, forBlockScope.FindIdentifier("n"));
            Assert.Same(forBlockScope, forBlockScope.FindIdentifier("x"));
            Assert.Same(wrapperFunctionBlockScope, wrapperFunctionBlockScope.FindIdentifier("y"));
            Assert.Null(forScope.FindIdentifier("f"));
            Assert.Same(wrapperFunctionBlockScope, forBlockScope.FindIdentifier("a"));
            Assert.Same(wrapperFunctionBlockScope, forBlockScope.FindIdentifier("b"));

            // for in

            ForInStatement forInStatement = wrapperFunctionBody.Body.OfType<ForInStatement>().Single();

            VariableScope forInScope = GetScope(forInStatement);
            Assert.Same(wrapperFunctionBlockScope, forInScope.ParentScope);

            Assert.Same(wrapperFunctionBlockScope, forInScope.FindIdentifier("i"));
            Assert.Same(wrapperFunctionBlockScope, forInScope.FindIdentifier("n"));
            Assert.Same(forInScope, forInScope.FindIdentifier("x"));
            Assert.Same(forInScope, forInScope.FindIdentifier("y"));
            Assert.Null(forInScope.FindIdentifier("f"));
            Assert.Same(wrapperFunctionBlockScope, forInScope.FindIdentifier("a"));
            Assert.Same(wrapperFunctionBlockScope, forInScope.FindIdentifier("b"));

            blockStatement = forInStatement.Body.As<NestedBlockStatement>();

            VariableScope forInBlockScope = GetScope(blockStatement);
            Assert.Same(forInScope, forInBlockScope.ParentScope);

            Assert.Same(wrapperFunctionBlockScope, forInBlockScope.FindIdentifier("i"));
            Assert.Same(wrapperFunctionBlockScope, forInBlockScope.FindIdentifier("n"));
            Assert.Same(forInBlockScope, forInBlockScope.FindIdentifier("x"));
            Assert.Same(forInBlockScope, forInBlockScope.FindIdentifier("y"));
            Assert.Null(forInBlockScope.FindIdentifier("f"));
            Assert.Same(wrapperFunctionBlockScope, forInBlockScope.FindIdentifier("a"));
            Assert.Same(wrapperFunctionBlockScope, forInBlockScope.FindIdentifier("b"));

            // for of

            ForOfStatement forOfStatement = wrapperFunctionBody.Body.OfType<ForOfStatement>().Single();

            VariableScope forOfScope = GetScope(forOfStatement);
            Assert.Same(wrapperFunctionBlockScope, forOfScope.ParentScope);

            Assert.Same(wrapperFunctionBlockScope, forOfScope.FindIdentifier("i"));
            Assert.Same(wrapperFunctionBlockScope, forOfScope.FindIdentifier("n"));
            Assert.Null(forOfScope.FindIdentifier("x"));
            Assert.Same(forOfScope, forOfScope.FindIdentifier("y"));
            Assert.Null(forOfScope.FindIdentifier("f"));
            Assert.Same(forOfScope, forOfScope.FindIdentifier("a"));
            Assert.Same(forOfScope, forOfScope.FindIdentifier("b"));

            Assert.Null(GetScope(forOfStatement.Body));
        }

        [Fact]
        public void FindIdentifer_SwitchBlockScope()
        {
            var moduleContent =
@"let a = 'tl_a';
(() => {
  let b = 'fn_b';

  switch (true) {
    case false: let a = 'sw_a'
    case true: const c = 'sw_c'
    default: var d = 'sw_d'
  } 
})()
";

            Module moduleAst = new Parser(ModuleBundler.CreateParserOptions()).ParseModule(moduleContent);

            var scopeBuilder = new VariableScopeBuilder();
            scopeBuilder.Visit(moduleAst);

            VariableScope topLevelScope = GetScope(moduleAst);

            Assert.Same(topLevelScope, topLevelScope.FindIdentifier("a"));
            Assert.Null(topLevelScope.FindIdentifier("b"));
            Assert.Null(topLevelScope.FindIdentifier("c"));
            Assert.Null(topLevelScope.FindIdentifier("d"));

            ArrowFunctionExpression functionExpression = moduleAst.Body[1]
                .As<NonSpecialExpressionStatement>().Expression
                .As<CallExpression>().Callee
                .As<ArrowFunctionExpression>();

            VariableScope wrapperFunctionScope = GetScope(functionExpression);
            Assert.Same(topLevelScope, wrapperFunctionScope.ParentScope);

            Assert.Same(topLevelScope, wrapperFunctionScope.FindIdentifier("a"));
            Assert.Null(wrapperFunctionScope.FindIdentifier("b"));
            Assert.Null(wrapperFunctionScope.FindIdentifier("c"));
            Assert.Null(wrapperFunctionScope.FindIdentifier("d"));

            FunctionBody wrapperFunctionBody = functionExpression.Body.As<FunctionBody>();

            VariableScope wrapperFunctionBlockScope = GetScope(wrapperFunctionBody);
            Assert.Same(wrapperFunctionScope, wrapperFunctionBlockScope.ParentScope);

            Assert.Same(topLevelScope, wrapperFunctionBlockScope.FindIdentifier("a"));
            Assert.Same(wrapperFunctionBlockScope, wrapperFunctionBlockScope.FindIdentifier("b"));
            Assert.Null(wrapperFunctionBlockScope.FindIdentifier("c"));
            Assert.Same(wrapperFunctionBlockScope, wrapperFunctionBlockScope.FindIdentifier("d"));

            SwitchStatement switchStatement = wrapperFunctionBody.Body[1].As<SwitchStatement>();

            VariableScope switchStatementBlockScope = GetScope(switchStatement);
            Assert.Same(wrapperFunctionBlockScope, switchStatementBlockScope.ParentScope);

            Assert.Same(switchStatementBlockScope, switchStatementBlockScope.FindIdentifier("a"));
            Assert.Same(wrapperFunctionBlockScope, switchStatementBlockScope.FindIdentifier("b"));
            Assert.Same(switchStatementBlockScope, switchStatementBlockScope.FindIdentifier("c"));
            Assert.Same(switchStatementBlockScope, switchStatementBlockScope.FindIdentifier("d"));
        }

        [Fact]
        public void FindIdentifer_CatchClauseScope()
        {
            var moduleContent =
@"(() => {
  let msg = '';
  
  try { throw {} }
  catch {
    console.log(msg, err);
  }
  
  try { throw {} }
  catch ({msg = 'error'}) {
    var err = {msg};
    console.log(msg, err);
  }
})()
";

            Module moduleAst = new Parser(ModuleBundler.CreateParserOptions()).ParseModule(moduleContent);

            var scopeBuilder = new VariableScopeBuilder();
            scopeBuilder.Visit(moduleAst);

            VariableScope topLevelScope = GetScope(moduleAst);

            ArrowFunctionExpression functionExpression = moduleAst.Body.Single()
                .As<NonSpecialExpressionStatement>().Expression
                .As<CallExpression>().Callee
                .As<ArrowFunctionExpression>();

            VariableScope wrapperFunctionScope = GetScope(functionExpression);
            Assert.Same(topLevelScope, wrapperFunctionScope.ParentScope);

            FunctionBody wrapperFunctionBody = functionExpression.Body.As<FunctionBody>();

            VariableScope wrapperFunctionBlockScope = GetScope(wrapperFunctionBody);
            Assert.Same(wrapperFunctionScope, wrapperFunctionBlockScope.ParentScope);

            Assert.Same(wrapperFunctionBlockScope, wrapperFunctionBlockScope.FindIdentifier("msg"));
            Assert.Same(wrapperFunctionBlockScope, wrapperFunctionBlockScope.FindIdentifier("err"));

            // catch clause #1

            CatchClause catchClause = wrapperFunctionBody.Body.OfType<TryStatement>().ElementAt(0).Handler;

            VariableScope catchClauseScope = GetScope(catchClause);
            Assert.Same(wrapperFunctionBlockScope, catchClauseScope.ParentScope);

            Assert.Same(wrapperFunctionBlockScope, catchClauseScope.FindIdentifier("msg"));
            Assert.Same(wrapperFunctionBlockScope, catchClauseScope.FindIdentifier("err"));

            VariableScope catchClauseBlockScope = GetScope(catchClause.Body);
            Assert.Same(catchClauseScope, catchClauseBlockScope.ParentScope);

            Assert.Same(wrapperFunctionBlockScope, catchClauseBlockScope.FindIdentifier("msg"));
            Assert.Same(wrapperFunctionBlockScope, catchClauseBlockScope.FindIdentifier("err"));

            // catch clause #2

            catchClause = wrapperFunctionBody.Body.OfType<TryStatement>().ElementAt(1).Handler;

            catchClauseScope = GetScope(catchClause);
            Assert.Same(wrapperFunctionBlockScope, catchClauseScope.ParentScope);

            Assert.Same(catchClauseScope, catchClauseScope.FindIdentifier("msg"));
            Assert.Same(catchClauseScope, catchClauseScope.FindIdentifier("err"));

            catchClauseBlockScope = GetScope(catchClause.Body);
            Assert.Same(catchClauseScope, catchClauseBlockScope.ParentScope);

            Assert.Same(catchClauseScope, catchClauseBlockScope.FindIdentifier("msg"));
            Assert.Same(catchClauseBlockScope, catchClauseBlockScope.FindIdentifier("err"));
        }

        [Fact]
        public void FindIdentifer_Hoisting()
        {
            var moduleContent =
@"const foo = 0;

function f1(a = foo) {
  {
    var foo = {};
  }
}

function f2(a = foo) {
  {
    function foo() { return this; }
  }
}

function f3(a) {
  'use strict';
  {
    function foo() { return this; }
  }
}

function f4(a = foo) {
  {
    class foo { }
  }
}
";

            Script moduleAst = new Parser(ModuleBundler.CreateParserOptions()).ParseScript(moduleContent);

            var scopeBuilder = new VariableScopeBuilder();
            scopeBuilder.Visit(moduleAst);

            VariableScope topLevelScope = GetScope(moduleAst);

            Assert.Same(topLevelScope, topLevelScope.FindIdentifier("foo"));

            // f1

            FunctionDeclaration functionDeclaration = moduleAst.Body
                .OfType<FunctionDeclaration>().Single(d => d.Id?.Name == "f1");

            VariableScope functionScope = GetScope(functionDeclaration);
            Assert.Same(topLevelScope, functionScope.ParentScope);

            Assert.Same(topLevelScope, functionScope.FindIdentifier("foo"));

            FunctionBody functionBody = functionDeclaration.Body;

            VariableScope functionBlockScope = GetScope(functionBody);
            Assert.Same(functionScope, functionBlockScope.ParentScope);

            Assert.Same(functionBlockScope, functionBlockScope.FindIdentifier("foo"));

            NestedBlockStatement blockStatement = functionBody.Body.OfType<NestedBlockStatement>().Single();

            VariableScope nestedBlockScope = GetScope(blockStatement);
            Assert.Same(functionBlockScope, nestedBlockScope.ParentScope);

            Assert.Same(nestedBlockScope, nestedBlockScope.FindIdentifier("foo"));

            // f2

            functionDeclaration = moduleAst.Body
                .OfType<FunctionDeclaration>().Single(d => d.Id?.Name == "f2");

            functionScope = GetScope(functionDeclaration);
            Assert.Same(topLevelScope, functionScope.ParentScope);

            Assert.Same(topLevelScope, functionScope.FindIdentifier("foo"));

            functionBody = functionDeclaration.Body;

            functionBlockScope = GetScope(functionBody);
            Assert.Same(functionScope, functionBlockScope.ParentScope);

            Assert.Same(functionBlockScope, functionBlockScope.FindIdentifier("foo"));

            blockStatement = functionBody.Body.OfType<NestedBlockStatement>().Single();

            nestedBlockScope = GetScope(blockStatement);
            Assert.Same(functionBlockScope, nestedBlockScope.ParentScope);

            Assert.Same(nestedBlockScope, nestedBlockScope.FindIdentifier("foo"));

            // f3

            functionDeclaration = moduleAst.Body
                .OfType<FunctionDeclaration>().Single(d => d.Id?.Name == "f3");

            functionScope = GetScope(functionDeclaration);
            Assert.Same(topLevelScope, functionScope.ParentScope);

            Assert.Same(topLevelScope, functionScope.FindIdentifier("foo"));

            functionBody = functionDeclaration.Body;

            functionBlockScope = GetScope(functionBody);
            Assert.Same(functionScope, functionBlockScope.ParentScope);

            Assert.Same(topLevelScope, functionBlockScope.FindIdentifier("foo"));

            blockStatement = functionBody.Body.OfType<NestedBlockStatement>().Single();

            nestedBlockScope = GetScope(blockStatement);
            Assert.Same(functionBlockScope, nestedBlockScope.ParentScope);

            Assert.Same(nestedBlockScope, nestedBlockScope.FindIdentifier("foo"));

            // f4

            functionDeclaration = moduleAst.Body
                .OfType<FunctionDeclaration>().Single(d => d.Id?.Name == "f4");

            functionScope = GetScope(functionDeclaration);
            Assert.Same(topLevelScope, functionScope.ParentScope);

            Assert.Same(topLevelScope, functionScope.FindIdentifier("foo"));

            functionBody = functionDeclaration.Body;

            functionBlockScope = GetScope(functionBody);
            Assert.Same(functionScope, functionBlockScope.ParentScope);

            Assert.Same(topLevelScope, functionBlockScope.FindIdentifier("foo"));

            blockStatement = functionBody.Body.OfType<NestedBlockStatement>().Single();

            nestedBlockScope = GetScope(blockStatement);
            Assert.Same(functionBlockScope, nestedBlockScope.ParentScope);

            Assert.Same(nestedBlockScope, nestedBlockScope.FindIdentifier("foo"));
        }

        [Fact]
        public void FindIdentifer_Hoisting_CatchClause()
        {
            var moduleContent =
@"function f1() {
  try { throw [0] }
  catch ([e]) { 
    { function e() { } } 
  }
  console.log(e)
}

function f2() {
  try { throw 0 }
  catch (e) { 
    { function e() { } } 
  }
  console.log(e)
}
";

            Script moduleAst = new Parser(ModuleBundler.CreateParserOptions()).ParseScript(moduleContent);

            var scopeBuilder = new VariableScopeBuilder();
            scopeBuilder.Visit(moduleAst);

            VariableScope topLevelScope = GetScope(moduleAst);

            Assert.Null(topLevelScope.FindIdentifier("e"));

            // f1

            FunctionDeclaration functionDeclaration = moduleAst.Body
                .OfType<FunctionDeclaration>().Single(d => d.Id?.Name == "f1");

            VariableScope functionScope = GetScope(functionDeclaration);
            Assert.Same(topLevelScope, functionScope.ParentScope);

            Assert.Null(functionScope.FindIdentifier("e"));

            FunctionBody functionBody = functionDeclaration.Body;

            VariableScope functionBlockScope = GetScope(functionBody);
            Assert.Same(functionScope, functionBlockScope.ParentScope);

            Assert.Null(functionBlockScope.FindIdentifier("e"));

            // f2

            functionDeclaration = moduleAst.Body
                .OfType<FunctionDeclaration>().Single(d => d.Id?.Name == "f2");

            functionScope = GetScope(functionDeclaration);
            Assert.Same(topLevelScope, functionScope.ParentScope);

            Assert.Null(functionScope.FindIdentifier("e"));

            functionBody = functionDeclaration.Body;

            functionBlockScope = GetScope(functionBody);
            Assert.Same(functionScope, functionBlockScope.ParentScope);

            Assert.Same(functionBlockScope, functionBlockScope.FindIdentifier("e"));
        }
    }
}
