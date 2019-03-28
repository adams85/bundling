(function (modules) {
    var moduleCache = {};

    function __es$require(moduleId) {
        var module = moduleCache[moduleId];
        if (!module) {
            var module = {
                id: moduleId,
                exports: {}
            };

            modules[moduleId].call(module.exports, __es$require, module.exports);
            moduleCache[moduleId] = module;
        }
        return module.exports;
    }

    __es$require.d = function (exports, name, getter) {
        if (!Object.prototype.hasOwnProperty.call(exports, name))
            Object.defineProperty(exports, name, { enumerable: true, get: getter });
    };

    __es$require("/ts/main.js");
})({
    /*** Module: PhysicalFileProvider[E:\Dev\Karambolo.AspNetCore.Bundling\samples\TypeScriptDemo\wwwroot\]:/ts/main.js *** //ts/main.js ***/
    "/ts/main.js": function (__es$require, __es$exports) {
/* <---- */
'use strict';

/* Imports */
var __es$module_0 = __es$require("/ts/foo.js");

var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : new P(function (resolve) { resolve(result.value); }).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
class App {
    constructor() {
        this._textBox = document.getElementById('textbox');
        this._textBox.value = __es$module_0.counter.toString();
        this._button = document.getElementById('button');
        this._button.onclick = () => this.click();
    }
    // you can use post ES6 features,
    // just make sure that the TypeScript compiler targets ES6
    // (see TypeScriptDemo.csproj)
    click() {
        return __awaiter(this, void 0, void 0, function* () {
            this._textBox.value = 'Thinking hard...';
            this._button.setAttribute('disabled', null);
            yield __es$module_0.timeout(1000);
            __es$module_0.incCounter();
            this._textBox.value = __es$module_0.counter.toString();
            this._button.removeAttribute('disabled');
        });
    }
}
new App();
//# sourceMappingURL=main.js.map
/* ----> */ },
    /*** Module: PhysicalFileProvider[E:\Dev\Karambolo.AspNetCore.Bundling\samples\TypeScriptDemo\wwwroot\]:/ts/foo.js *** //ts/foo.js ***/
    "/ts/foo.js": function (__es$require, __es$exports) {
/* <---- */
'use strict';

/* Imports */
var __es$module_0 = __es$require("/ts/bar.js");

/* Exports */
__es$require.d(__es$exports, "counter", function() { return __es$module_0.counter; });
__es$require.d(__es$exports, "incCounter", function() { return __es$module_0.incCounter; });
__es$require.d(__es$exports, "timeout", function() { return _timeout; });

// re-exports everything from bar
// exports a functions
function _timeout(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}
//# sourceMappingURL=foo.js.map
/* ----> */ },
    /*** Module: PhysicalFileProvider[E:\Dev\Karambolo.AspNetCore.Bundling\samples\TypeScriptDemo\wwwroot\]:/ts/bar.js *** //ts/bar.js ***/
    "/ts/bar.js": function (__es$require, __es$exports) {
/* <---- */
'use strict';

/* Exports */
__es$require.d(__es$exports, "counter", function() { return counter; });
__es$require.d(__es$exports, "incCounter", function() { return incCounter; });

// http://exploringjs.com/es6/ch_modules.html#_imports-are-read-only-views-on-exports
let counter = 0;
function incCounter() {
    counter++;
}
//# sourceMappingURL=bar.js.map
/* ----> */ },
});
