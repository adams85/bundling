var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
import { counter, incCounter, timeout } from './foo';
class App {
    constructor() {
        this._textBox = document.getElementById('textbox');
        this._textBox.value = counter.toString();
        this._button = document.getElementById('button');
        this._button.onclick = () => this.click();
    }
    // you may use post-ES6 features,
    // just make sure that the TypeScript compiler targets ES6
    // (see TypeScriptDemo.csproj)
    click() {
        return __awaiter(this, void 0, void 0, function* () {
            this._textBox.value = 'Thinking hard...';
            this._button.setAttribute('disabled', null);
            yield timeout(1000);
            incCounter();
            this._textBox.value = counter.toString();
            this._button.removeAttribute('disabled');
        });
    }
}
new App();
//# sourceMappingURL=main.js.map