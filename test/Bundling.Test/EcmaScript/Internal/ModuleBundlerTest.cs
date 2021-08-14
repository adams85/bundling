using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Karambolo.AspNetCore.Bundling.Test.Helpers;
using Xunit;

namespace Karambolo.AspNetCore.Bundling.EcmaScript.Internal
{
    public class ModuleBundlerTest
    {
        private static string[] GetNonEmptyLines(string content)
        {
            string line;
            var lines = new List<string>();
            using (var reader = new StringReader(content))
                while ((line = reader.ReadLine()) != null)
                    if (!string.IsNullOrWhiteSpace(line))
                        lines.Add(line);

            return lines.ToArray();
        }

        [Fact]
        public async Task Export_Named_Inline()
        {
            var fooContent =
@"import * as bar from './bar';";

            var barContent =
@"export var myVar1 = '1';
export let myVar2 = 2;
export const MY_CONST = 3.14;
export function myFunc() { return myVar1; }
export function* myGeneratorFunc() { yield myVar2; }
export class MyClass { method() { return myFunc(); } }";

            var fileProvider = new MemoryFileProvider();
            fileProvider.CreateFile("/bar.js", barContent);

            var fooFile = new ModuleFile(fileProvider, null) { Content = fooContent };

            var moduleBundler = new ModuleBundler();

            await moduleBundler.BundleCoreAsync(new[] { fooFile }, CancellationToken.None);

            var barLines = GetNonEmptyLines(moduleBundler.Modules.Single(kvp => kvp.Key.FilePath == "/bar.js").Value.Content);
            Assert.Equal(new[]
            {
                "'use strict';",
                "__es$require.d(__es$exports, \"myVar1\", function() { return myVar1; });",
                "__es$require.d(__es$exports, \"myVar2\", function() { return myVar2; });",
                "__es$require.d(__es$exports, \"MY_CONST\", function() { return MY_CONST; });",
                "__es$require.d(__es$exports, \"myFunc\", function() { return myFunc; });",
                "__es$require.d(__es$exports, \"myGeneratorFunc\", function() { return myGeneratorFunc; });",
                "__es$require.d(__es$exports, \"MyClass\", function() { return MyClass; });",
                "var myVar1 = '1';",
                "let myVar2 = 2;",
                "const MY_CONST = 3.14;",
                "function myFunc() { return myVar1; }",
                "function* myGeneratorFunc() { yield myVar2; }",
                "class MyClass { method() { return myFunc(); } }",
            }, barLines);
        }

        [Fact]
        public async Task Export_Named_Clause()
        {
            var fooContent =
@"import * as bar from './bar';";

            var barContent =
@"var myVar1 = '1';
let myVar2 = 2;
const MY_CONST = 3.14;
function myFunc() { return myVar1; }
function* myGeneratorFunc() { yield myVar2; }
class MyClass { method() { return myFunc(); } }

export { myVar1, myVar2 as var2, MY_CONST, myFunc, myGeneratorFunc, MyClass as default }";

            var fileProvider = new MemoryFileProvider();
            fileProvider.CreateFile("/bar.js", barContent);

            var fooFile = new ModuleFile(fileProvider, null) { Content = fooContent };

            var moduleBundler = new ModuleBundler();

            await moduleBundler.BundleCoreAsync(new[] { fooFile }, CancellationToken.None);

            var barLines = GetNonEmptyLines(moduleBundler.Modules.Single(kvp => kvp.Key.FilePath == "/bar.js").Value.Content);
            Assert.Equal(new[]
            {
                "'use strict';",
                "__es$require.d(__es$exports, \"myVar1\", function() { return myVar1; });",
                "__es$require.d(__es$exports, \"var2\", function() { return myVar2; });",
                "__es$require.d(__es$exports, \"MY_CONST\", function() { return MY_CONST; });",
                "__es$require.d(__es$exports, \"myFunc\", function() { return myFunc; });",
                "__es$require.d(__es$exports, \"myGeneratorFunc\", function() { return myGeneratorFunc; });",
                "__es$require.d(__es$exports, \"default\", function() { return MyClass; });",
                "var myVar1 = '1';",
                "let myVar2 = 2;",
                "const MY_CONST = 3.14;",
                "function myFunc() { return myVar1; }",
                "function* myGeneratorFunc() { yield myVar2; }",
                "class MyClass { method() { return myFunc(); } }",
            }, barLines);
        }

        [Fact]
        public async Task Export_Default_Expression()
        {
            var fooContent =
@"import * as bar from './bar';";

            var barContent =
@"let x;

export default x = 3 * 7;
x = 0;";

            var fileProvider = new MemoryFileProvider();
            fileProvider.CreateFile("/bar.js", barContent);

            var fooFile = new ModuleFile(fileProvider, null) { Content = fooContent };

            var moduleBundler = new ModuleBundler();

            await moduleBundler.BundleCoreAsync(new[] { fooFile }, CancellationToken.None);

            var barLines = GetNonEmptyLines(moduleBundler.Modules.Single(kvp => kvp.Key.FilePath == "/bar.js").Value.Content);
            Assert.Equal(new[]
            {
                "'use strict';",
                "__es$require.d(__es$exports, \"default\", function() { return __es$default; });",
                "let x;",
                "var __es$default = x = 3 * 7;",
                "x = 0;",
            }, barLines);
        }

        [Fact]
        public async Task Export_Default_Declaration()
        {
            var fooContent =
@"import * as bar from './bar';
export let x = 0;";

            var barContent =
@"import { x } from './foo';

export default /***/ ( /***/ class { m() { return x } }  /***/ )";

            var fileProvider = new MemoryFileProvider();
            fileProvider.CreateFile("/foo.js", fooContent);
            fileProvider.CreateFile("/bar.js", barContent);

            var moduleBundler = new ModuleBundler();

            await moduleBundler.BundleCoreAsync(new[] { new ModuleFile(fileProvider, "/foo.js") }, CancellationToken.None);

            var barLines = GetNonEmptyLines(moduleBundler.Modules.Single(kvp => kvp.Key.FilePath == "/bar.js").Value.Content);
            Assert.Equal(new[]
            {
                "'use strict';",
                "var __es$module_0 = __es$require(\"/foo.js\");",
                "__es$require.d(__es$exports, \"default\", function() { return __es$default; });",
                "var __es$default = ( /***/ class { m() { return __es$module_0.x } }  /***/ )",
            }, barLines);
        }

        [Fact]
        public async Task Export_Default_Named()
        {
            var fooContent =
@"import * as bar from './bar';";

            var barContent =
@"export default function myFunc() {}";

            var fileProvider = new MemoryFileProvider();
            fileProvider.CreateFile("/bar.js", barContent);

            var fooFile = new ModuleFile(fileProvider, null) { Content = fooContent };

            var moduleBundler = new ModuleBundler();

            await moduleBundler.BundleCoreAsync(new[] { fooFile }, CancellationToken.None);

            var barLines = GetNonEmptyLines(moduleBundler.Modules.Single(kvp => kvp.Key.FilePath == "/bar.js").Value.Content);
            Assert.Equal(new[]
            {
                "'use strict';",
                "__es$require.d(__es$exports, \"default\", function() { return myFunc; });",
                "function myFunc() {}",
            }, barLines);
        }

        [Fact]
        public async Task Reexport_Everything()
        {
            var fooContent =
@"import * as bar from './bar';";

            var barContent =
@"export * from './baz'";

            var bazContent =
@"export /***/ var [myVar1, { a: myVar2, b: { c: myVar3, ...rest1} }, ...rest2] = [1, { a: 2, b: { c: 3.14 } }];";

            var fileProvider = new MemoryFileProvider();
            fileProvider.CreateFile("/bar.js", barContent);
            fileProvider.CreateFile("/baz.js", bazContent);

            var fooFile = new ModuleFile(fileProvider, null) { Content = fooContent };

            var moduleBundler = new ModuleBundler();

            await moduleBundler.BundleCoreAsync(new[] { fooFile }, CancellationToken.None);

            var barLines = GetNonEmptyLines(moduleBundler.Modules.Single(kvp => kvp.Key.FilePath == "/bar.js").Value.Content);
            Assert.Equal(new[]
            {
                "'use strict';",
                "var __es$module_0 = __es$require(\"/baz.js\");",
                "__es$require.d(__es$exports, \"myVar1\", function() { return __es$module_0.myVar1; });",
                "__es$require.d(__es$exports, \"myVar2\", function() { return __es$module_0.myVar2; });",
                "__es$require.d(__es$exports, \"myVar3\", function() { return __es$module_0.myVar3; });",
                "__es$require.d(__es$exports, \"rest1\", function() { return __es$module_0.rest1; });",
                "__es$require.d(__es$exports, \"rest2\", function() { return __es$module_0.rest2; });",
            }, barLines);

            var bazLines = GetNonEmptyLines(moduleBundler.Modules.Single(kvp => kvp.Key.FilePath == "/baz.js").Value.Content);
            Assert.Equal(new[]
            {
                "'use strict';",
                "__es$require.d(__es$exports, \"myVar1\", function() { return myVar1; });",
                "__es$require.d(__es$exports, \"myVar2\", function() { return myVar2; });",
                "__es$require.d(__es$exports, \"myVar3\", function() { return myVar3; });",
                "__es$require.d(__es$exports, \"rest1\", function() { return rest1; });",
                "__es$require.d(__es$exports, \"rest2\", function() { return rest2; });",
                "var [myVar1, { a: myVar2, b: { c: myVar3, ...rest1} }, ...rest2] = [1, { a: 2, b: { c: 3.14 } }];",
            }, bazLines);
        }

        [Fact]
        public async Task Reexport_Clause()
        {
            var fooContent =
@"import * as bar from './bar';";

            var barContent =
@"export { default, default as defaultAlias, rest, rest as restAlias } from './baz'";

            var bazContent =
@"var [myVar1, {a: myVar2, b: [myVar3, ...rest]}] = [1, {a: 2, b: [3.14]}];
export { myVar1 as default, rest };";

            var fileProvider = new MemoryFileProvider();
            fileProvider.CreateFile("/bar.js", barContent);
            fileProvider.CreateFile("/baz.js", bazContent);

            var fooFile = new ModuleFile(fileProvider, null) { Content = fooContent };

            var moduleBundler = new ModuleBundler();

            await moduleBundler.BundleCoreAsync(new[] { fooFile }, CancellationToken.None);

            var barLines = GetNonEmptyLines(moduleBundler.Modules.Single(kvp => kvp.Key.FilePath == "/bar.js").Value.Content);
            Assert.Equal(new[]
            {
                "'use strict';",
                "var __es$module_0 = __es$require(\"/baz.js\");",
                "__es$require.d(__es$exports, \"default\", function() { return __es$module_0.default; });",
                "__es$require.d(__es$exports, \"defaultAlias\", function() { return __es$module_0.default; });",
                "__es$require.d(__es$exports, \"rest\", function() { return __es$module_0.rest; });",
                "__es$require.d(__es$exports, \"restAlias\", function() { return __es$module_0.rest; });",
            }, barLines);

            var bazLines = GetNonEmptyLines(moduleBundler.Modules.Single(kvp => kvp.Key.FilePath == "/baz.js").Value.Content);
            Assert.Equal(new[]
            {
                "'use strict';",
                "__es$require.d(__es$exports, \"default\", function() { return myVar1; });",
                "__es$require.d(__es$exports, \"rest\", function() { return rest; });",
                "var [myVar1, {a: myVar2, b: [myVar3, ...rest]}] = [1, {a: 2, b: [3.14]}];",
            }, bazLines);
        }

        [Fact]
        public async Task Import_Named()
        {
            var fooContent =
@"import defaultExport, { myVar1, myVar2 as var2 } from './bar';

console.log(defaultExport);
function func(myVar1) { console.log(myVar1 + var2); }
";

            var barContent =
@"export var myVar1 = '1';
export let myVar2 = 2;
export default 3.14;";

            var fileProvider = new MemoryFileProvider();
            fileProvider.CreateFile("/bar.js", barContent);

            var fooFile = new ModuleFile(fileProvider, null) { Content = fooContent };

            var moduleBundler = new ModuleBundler();

            await moduleBundler.BundleCoreAsync(new[] { fooFile }, CancellationToken.None);

            var fooLines = GetNonEmptyLines(moduleBundler.Modules.Single(kvp => kvp.Key.FilePath == "<root0>").Value.Content);
            Assert.Equal(new[]
            {
                "'use strict';",
                "var __es$module_0 = __es$require(\"/bar.js\");",
                "console.log(__es$module_0.default);",
                "function func(myVar1) { console.log(myVar1 + __es$module_0.myVar2); }",
            }, fooLines);
        }

        [Fact]
        public async Task Import_Namespace()
        {
            var fooContent =
@"import defaultExport, * as bar from './bar';

console.log(defaultExport);
var myVar1 = bar.myVar1;
console.log(myVar1 + bar.myVar2);
";

            var barContent =
@"export var myVar1 = '1';
export let myVar2 = 2;
export default 3.14;";

            var fileProvider = new MemoryFileProvider();
            fileProvider.CreateFile("/bar.js", barContent);

            var fooFile = new ModuleFile(fileProvider, null) { Content = fooContent };

            var moduleBundler = new ModuleBundler();

            await moduleBundler.BundleCoreAsync(new[] { fooFile }, CancellationToken.None);

            var fooLines = GetNonEmptyLines(moduleBundler.Modules.Single(kvp => kvp.Key.FilePath == "<root0>").Value.Content);
            Assert.Equal(new[]
            {
                "'use strict';",
                "var __es$module_0 = __es$require(\"/bar.js\");",
                "console.log(__es$module_0.default);",
                "var myVar1 = __es$module_0.myVar1;",
                "console.log(myVar1 + __es$module_0.myVar2);",
            }, fooLines);
        }

        [Fact]
        public async Task Import_CircularReference()
        {
            var fooContent =
@"export var fooVar = 1;
import { barVar } from './bar';
console.log({fooVar, barVar})";

            var barContent =
@"export var barVar = 2;
import { fooVar } from './foo';
console.log({fooVar, barVar})";

            var fileProvider = new MemoryFileProvider();
            fileProvider.CreateFile("/foo.js", fooContent);
            fileProvider.CreateFile("/bar.js", barContent);

            var fooFile = new ModuleFile(fileProvider, "/foo.js");

            var moduleBundler = new ModuleBundler();

            await moduleBundler.BundleCoreAsync(new[] { fooFile }, CancellationToken.None);

            var fooLines = GetNonEmptyLines(moduleBundler.Modules.Single(kvp => kvp.Key.FilePath == "/foo.js").Value.Content);
            Assert.Equal(new[]
            {
                "'use strict';",
                "var __es$module_0 = __es$require(\"/bar.js\");",
                "__es$require.d(__es$exports, \"fooVar\", function() { return fooVar; });",
                "var fooVar = 1;",
                "console.log({fooVar, barVar: __es$module_0.barVar})",
            }, fooLines);

            var barLines = GetNonEmptyLines(moduleBundler.Modules.Single(kvp => kvp.Key.FilePath == "/bar.js").Value.Content);
            Assert.Equal(new[]
            {
                 "'use strict';",
                 "var __es$module_0 = __es$require(\"/foo.js\");",
                 "__es$require.d(__es$exports, \"barVar\", function() { return barVar; });",
                 "var barVar = 2;",
                 "console.log({fooVar: __es$module_0.fooVar, barVar})",
            }, barLines);
        }

        [Fact]
        public async Task Import_CircularReference_With_Reexports()
        {
            var fooContent =
@"export var fooVar = 1;
export * from './bar';
import * as bar from './bar';
console.log(bar.fooVar + bar.barVar + bar.bazVar)";

            var barContent =
@"export var barVar = 2;
export * from './baz';
import * as baz from './baz';
console.log(baz.fooVar + baz.barVar + baz.bazVar)";

            var bazContent =
@"export var bazVar = 3;
export * from './foo';
import * as foo from './foo';
console.log(foo.fooVar + foo.barVar + foo.bazVar)";

            var fileProvider = new MemoryFileProvider();
            fileProvider.CreateFile("/foo.js", fooContent);
            fileProvider.CreateFile("/bar.js", barContent);
            fileProvider.CreateFile("/baz.js", bazContent);

            var fooFile = new ModuleFile(fileProvider, "/foo.js");

            var moduleBundler = new ModuleBundler();

            await moduleBundler.BundleCoreAsync(new[] { fooFile }, CancellationToken.None);

            var fooLines = GetNonEmptyLines(moduleBundler.Modules.Single(kvp => kvp.Key.FilePath == "/foo.js").Value.Content);
            Assert.Equal(new[]
            {
                "'use strict';",
                "var __es$module_0 = __es$require(\"/bar.js\");",
                "__es$require.d(__es$exports, \"fooVar\", function() { return fooVar; });",
                "__es$require.d(__es$exports, \"barVar\", function() { return __es$module_0.barVar; });",
                "__es$require.d(__es$exports, \"bazVar\", function() { return __es$module_0.bazVar; });",
                "var fooVar = 1;",
                "console.log(__es$module_0.fooVar + __es$module_0.barVar + __es$module_0.bazVar)",
            }, fooLines);

            var barLines = GetNonEmptyLines(moduleBundler.Modules.Single(kvp => kvp.Key.FilePath == "/bar.js").Value.Content);
            Assert.Equal(new[]
            {
                "'use strict';",
                "var __es$module_0 = __es$require(\"/baz.js\");",
                "__es$require.d(__es$exports, \"barVar\", function() { return barVar; });",
                "__es$require.d(__es$exports, \"bazVar\", function() { return __es$module_0.bazVar; });",
                "__es$require.d(__es$exports, \"fooVar\", function() { return __es$module_0.fooVar; });",
                "var barVar = 2;",
                "console.log(__es$module_0.fooVar + __es$module_0.barVar + __es$module_0.bazVar)",
            }, barLines);

            var bazLines = GetNonEmptyLines(moduleBundler.Modules.Single(kvp => kvp.Key.FilePath == "/baz.js").Value.Content);
            Assert.Equal(new[]
            {
                "'use strict';",
                "var __es$module_0 = __es$require(\"/foo.js\");",
                "__es$require.d(__es$exports, \"bazVar\", function() { return bazVar; });",
                "__es$require.d(__es$exports, \"fooVar\", function() { return __es$module_0.fooVar; });",
                "__es$require.d(__es$exports, \"barVar\", function() { return __es$module_0.barVar; });",
                "var bazVar = 3;",
                "console.log(__es$module_0.fooVar + __es$module_0.barVar + __es$module_0.bazVar)",
            }, bazLines);
        }

        [Fact]
        public async Task Import_Multiple_Imports()
        {
            var foo1Content =
 @"import { myVar1 as var1, myVar2 as var2 } from './bar';
import { default as bazFunc } from './baz';
console.log(bazFunc(var1, var2));";

            var foo2Content =
 @"import * as bar from './bar';
import bazFunc from './baz';
class MyClass
{
    myMethod(bar) {
        console.log(bazFunc(bar.myVar1, bar.myVar2));
    }
}
new MyClass().myMethod(bar);
";

            var barContent =
@"export var myVar1 = '1';
export let myVar2 = 2;
export default 3.14;";

            var bazContent =
@"export default function MyFunc(a, b) { return a + b; }";

            var fileProvider = new MemoryFileProvider();
            fileProvider.CreateFile("/bar.js", barContent);
            fileProvider.CreateFile("/baz.js", bazContent);

            var foo1File = new ModuleFile(fileProvider, null) { Content = foo1Content };
            var foo2File = new ModuleFile(fileProvider, null) { Content = foo2Content };

            var moduleBundler = new ModuleBundler();

            await moduleBundler.BundleCoreAsync(new[] { foo1File, foo2File }, CancellationToken.None);

            var foo1Lines = GetNonEmptyLines(moduleBundler.Modules.Single(kvp => kvp.Key.FilePath == "<root0>").Value.Content);
            Assert.Equal(new[]
            {
                "'use strict';",
                 "var __es$module_0 = __es$require(\"/bar.js\");",
                 "var __es$module_1 = __es$require(\"/baz.js\");",
                 "console.log(__es$module_1.default(__es$module_0.myVar1, __es$module_0.myVar2));",
            }, foo1Lines);

            var foo2Lines = GetNonEmptyLines(moduleBundler.Modules.Single(kvp => kvp.Key.FilePath == "<root1>").Value.Content);
            Assert.Equal(new[]
            {
                "'use strict';",
                "var __es$module_0 = __es$require(\"/bar.js\");",
                "var __es$module_1 = __es$require(\"/baz.js\");",
                "class MyClass",
                "{",
                "    myMethod(bar) {",
                "        console.log(__es$module_1.default(bar.myVar1, bar.myVar2));",
                "    }",
                "}",
                "new MyClass().myMethod(__es$module_0);",
            }, foo2Lines);
        }

        [Fact]
        public async Task Import_SpecialPropertiesAndMethods()
        {
            var fooContent =
 @"import { someKey } from './bar';

let o = { someKey, [someKey]: 0 };
o = { someKey() {}, [someKey]() {} };
o = { *someKey() {}, *[someKey]() {} };
o = { get someKey() {}, get [someKey]() {} };
o = { set someKey(_) {}, set [someKey](_) {} };
class C1 { 
  [someKey]() {}
  get someKey() {}
  set someKey(_) {}
  get [someKey]() {}
  set [someKey](_) {}
}
class C2 {
  *someKey() {}
  *[someKey]() {} 
}
class C3 {
  static someKey() {}
  static [someKey]() {} 
}";

            var barContent =
@"export const propKey = 'propKey';";

            var fileProvider = new MemoryFileProvider();
            fileProvider.CreateFile("/bar.js", barContent);

            var fooFile = new ModuleFile(fileProvider, null) { Content = fooContent };

            var moduleBundler = new ModuleBundler();

            await moduleBundler.BundleCoreAsync(new[] { fooFile }, CancellationToken.None);

            var fooLines = GetNonEmptyLines(moduleBundler.Modules.Single(kvp => kvp.Key.FilePath == "<root0>").Value.Content);
            Assert.Equal(new[]
            {
                "'use strict';",
                "var __es$module_0 = __es$require(\"/bar.js\");",
                "let o = { someKey: __es$module_0.someKey, [__es$module_0.someKey]: 0 };",
                "o = { someKey() {}, [__es$module_0.someKey]() {} };",
                "o = { *someKey() {}, *[__es$module_0.someKey]() {} };",
                "o = { get someKey() {}, get [__es$module_0.someKey]() {} };",
                "o = { set someKey(_) {}, set [__es$module_0.someKey](_) {} };",
                "class C1 { ",
                "  [__es$module_0.someKey]() {}",
                "  get someKey() {}",
                "  set someKey(_) {}",
                "  get [__es$module_0.someKey]() {}",
                "  set [__es$module_0.someKey](_) {}",
                "}",
                "class C2 {",
                "  *someKey() {}",
                "  *[__es$module_0.someKey]() {} ",
                "}",
                "class C3 {",
                "  static someKey() {}",
                "  static [__es$module_0.someKey]() {} ",
                "}",
            }, fooLines);
        }

        [Fact]
        public async Task VariableScoping()
        {
            var fooContent =
@"import foo from './bar';

function f1({[foo.key]: a} = {[foo.key]: foo.value}) {
  { let foo = {key: 'k2', value: 'v2'}; }
  console.log(a, foo);
}
function f2({[foo.key]: a} = {[foo.key]: foo.value}) {
  { var foo = {key: 'k2', value: 'v2'}; }
  console.log(a, foo);
}
function f3(foo = {key: 'k2', value: 'v2'}, {[foo.key]: a} = {[foo.key]: foo.value}) {
  console.log(a, foo);
}
";

            var barContent =
@"let foo;
export default foo = {key: 'k', value: 'v'};
";

            var fileProvider = new MemoryFileProvider();
            fileProvider.CreateFile("/bar.js", barContent);

            var fooFile = new ModuleFile(fileProvider, "/foo.js") { Content = fooContent };

            var moduleBundler = new ModuleBundler();

            await moduleBundler.BundleCoreAsync(new[] { fooFile }, CancellationToken.None);

            var fooLines = GetNonEmptyLines(moduleBundler.Modules.Single(kvp => kvp.Key.FilePath == "/foo.js").Value.Content);
            Assert.Equal(new[]
            {
                "'use strict';",
                "var __es$module_0 = __es$require(\"/bar.js\");",
                "function f1({[__es$module_0.default.key]: a} = {[__es$module_0.default.key]: __es$module_0.default.value}) {",
                "  { let foo = {key: 'k2', value: 'v2'}; }",
                "  console.log(a, __es$module_0.default);",
                "}",
                "function f2({[__es$module_0.default.key]: a} = {[__es$module_0.default.key]: __es$module_0.default.value}) {",
                "  { var foo = {key: 'k2', value: 'v2'}; }",
                "  console.log(a, foo);",
                "}",
                "function f3(foo = {key: 'k2', value: 'v2'}, {[foo.key]: a} = {[foo.key]: foo.value}) {",
                "  console.log(a, foo);",
                "}",
            }, fooLines);

            var barLines = GetNonEmptyLines(moduleBundler.Modules.Single(kvp => kvp.Key.FilePath == "/bar.js").Value.Content);
            Assert.Equal(new[]
            {
                "'use strict';",
                "__es$require.d(__es$exports, \"default\", function() { return __es$default; });",
                "let foo;",
                "var __es$default = foo = {key: 'k', value: 'v'};",
            }, barLines);
        }

        [Fact]
        public async Task Feature_AsyncAwait_And_DynamicImport()
        {
            // https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Operators/async_function
            // https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Operators/await
            // https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Statements/import#dynamic_imports

            var fooContent =
@"import * as bar from './bar';
(async () => await bar.myAsyncFunc3())();
";

            var barContent =
@"
export const myAsyncLambda = async () => await import('x');
export const myAsyncFunc1 = async function() { return await myAsyncLambda(); }
export async function myAsyncFunc2() { return await myAsyncFunc1(); }
async function myAsyncFunc3() { return await myAsyncFunc2(); }
export { myAsyncFunc3 }
";

            var fileProvider = new MemoryFileProvider();
            fileProvider.CreateFile("/bar.js", barContent);

            var fooFile = new ModuleFile(fileProvider, "/foo.js") { Content = fooContent };

            var moduleBundler = new ModuleBundler();

            await moduleBundler.BundleCoreAsync(new[] { fooFile }, CancellationToken.None);

            var fooLines = GetNonEmptyLines(moduleBundler.Modules.Single(kvp => kvp.Key.FilePath == "/foo.js").Value.Content);
            Assert.Equal(new[]
            {
                "'use strict';",
                "var __es$module_0 = __es$require(\"/bar.js\");",
                "(async () => await __es$module_0.myAsyncFunc3())();",
            }, fooLines);

            var barLines = GetNonEmptyLines(moduleBundler.Modules.Single(kvp => kvp.Key.FilePath == "/bar.js").Value.Content);
            Assert.Equal(new[]
            {
                "'use strict';",
                "__es$require.d(__es$exports, \"myAsyncLambda\", function() { return myAsyncLambda; });",
                "__es$require.d(__es$exports, \"myAsyncFunc1\", function() { return myAsyncFunc1; });",
                "__es$require.d(__es$exports, \"myAsyncFunc2\", function() { return myAsyncFunc2; });",
                "__es$require.d(__es$exports, \"myAsyncFunc3\", function() { return myAsyncFunc3; });",
                "const myAsyncLambda = async () => await import('x');",
                "const myAsyncFunc1 = async function() { return await myAsyncLambda(); }",
                "async function myAsyncFunc2() { return await myAsyncFunc1(); }",
                "async function myAsyncFunc3() { return await myAsyncFunc2(); }",
            }, barLines);
        }

        [Fact]
        public async Task Feature_Object_Rest_And_Spread_Properties()
        {
            // https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Operators/Spread_syntax#spread_in_object_literals
            // https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Operators/Object_initializer#spread_properties

            var fooContent =
@"import bar from './bar';
export var {myVarKey = bar.myVarKey, [bar.myVarKey]: barVar, myFunc: barFunc, ...rest} = {...bar};
";

            var barContent =
@"
export default ({ myVarKey: 'myVar', myVar: 10, myFunc: () => 20 });
";

            var fileProvider = new MemoryFileProvider();
            fileProvider.CreateFile("/bar.js", barContent);

            var fooFile = new ModuleFile(fileProvider, "/foo.js") { Content = fooContent };

            var moduleBundler = new ModuleBundler();

            await moduleBundler.BundleCoreAsync(new[] { fooFile }, CancellationToken.None);

            var fooLines = GetNonEmptyLines(moduleBundler.Modules.Single(kvp => kvp.Key.FilePath == "/foo.js").Value.Content);
            Assert.Equal(new[]
            {
                "'use strict';",
                "var __es$module_0 = __es$require(\"/bar.js\");",
                "__es$require.d(__es$exports, \"myVarKey\", function() { return myVarKey; });",
                "__es$require.d(__es$exports, \"barVar\", function() { return barVar; });",
                "__es$require.d(__es$exports, \"barFunc\", function() { return barFunc; });",
                "__es$require.d(__es$exports, \"rest\", function() { return rest; });",
                "var {myVarKey = __es$module_0.default.myVarKey, [__es$module_0.default.myVarKey]: barVar, myFunc: barFunc, ...rest} = {...__es$module_0.default};",
            }, fooLines);

            var barLines = GetNonEmptyLines(moduleBundler.Modules.Single(kvp => kvp.Key.FilePath == "/bar.js").Value.Content);
            Assert.Equal(new[]
            {
                "'use strict';",
                "__es$require.d(__es$exports, \"default\", function() { return __es$default; });",
                "var __es$default = ({ myVarKey: 'myVar', myVar: 10, myFunc: () => 20 });",
            }, barLines);
        }

        [Fact]
        public async Task Feature_ImportMeta()
        {
            // https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Statements/import.meta#using_import.meta

            var fooContent =
@"import { importUrl as barImportUrl } from './bar';
console.log(barImportUrl);
";

            var barContent =
@"
export const importUrl = import/*x*/
. /*y*/ meta.url;
";

            var fileProvider = new MemoryFileProvider();
            fileProvider.CreateFile("/bar.js", barContent);

            var fooFile = new ModuleFile(fileProvider, "/foo.js") { Content = fooContent };

            var moduleBundler = new ModuleBundler();

            await moduleBundler.BundleCoreAsync(new[] { fooFile }, CancellationToken.None);

            var fooLines = GetNonEmptyLines(moduleBundler.Modules.Single(kvp => kvp.Key.FilePath == "/foo.js").Value.Content);
            Assert.Equal(new[]
            {
                "'use strict';",
                "var __es$module_0 = __es$require(\"/bar.js\");",
                "console.log(__es$module_0.importUrl);",
            }, fooLines);

            var barLines = GetNonEmptyLines(moduleBundler.Modules.Single(kvp => kvp.Key.FilePath == "/bar.js").Value.Content);
            Assert.Equal(new[]
            {
                "'use strict';",
                "var __es$importMeta = { };",
                "__es$require.d(__es$importMeta, \"url\", function() { return \"provider-file:MemoryFileProvider/bar.js\"; });",
                "__es$require.d(__es$exports, \"importUrl\", function() { return importUrl; });",
                "const importUrl = __es$importMeta.url;",
            }, barLines);
        }

        [Fact]
        public async Task Issue11_NRE_When_Variable_Has_No_Initializer()
        {
            var fooContent =
@"
var i, length;
i = 1;
";

            var fileProvider = new MemoryFileProvider();

            var fooFile = new ModuleFile(fileProvider, "/foo.js") { Content = fooContent };

            var moduleBundler = new ModuleBundler();

            await moduleBundler.BundleCoreAsync(new[] { fooFile }, CancellationToken.None);

            var fooLines = GetNonEmptyLines(moduleBundler.Modules.Single(kvp => kvp.Key.FilePath == "/foo.js").Value.Content);
            Assert.Equal(new[]
            {
                "'use strict';",
                "var i, length;",
                "i = 1;",
            }, fooLines);
        }
    }
}
