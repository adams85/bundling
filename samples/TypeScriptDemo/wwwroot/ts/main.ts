import { counter, incCounter, timeout } from './foo.js';

class App {
    private _textBox: HTMLInputElement;
    private _button: HTMLInputElement;

    constructor() {
        this._textBox = <HTMLInputElement>document.getElementById('textbox');
        this._textBox.value = counter.toString();

        this._button = <HTMLInputElement>document.getElementById('button');
        this._button.onclick = () => this.click();
    }

    // you may use post-ES2020 language features,
    // just make sure that the TypeScript compiler targets ES2020
    // (see TypeScriptDemo.csproj)
    async click(): Promise<void> {
        this._textBox.value = 'Thinking hard...';
        this._button.setAttribute('disabled', null);

        await timeout(1000);

        incCounter();
        this._textBox.value = counter.toString();

        this._button.removeAttribute('disabled');
    }
}

new App();
