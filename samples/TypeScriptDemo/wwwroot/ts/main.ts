import { counter, incCounter, timeout } from './foo.js';

class App {
    private _textBox: HTMLInputElement;
    private _button: HTMLInputElement;

    constructor() {
        this._textBox = document.getElementById('textbox') as HTMLInputElement;
        this._textBox.value = counter.toString();

        this._button = document.getElementById('button') as HTMLInputElement;
        this._button.onclick = () => this.click();
    }

    // you may use post-ES2021 language features,
    // just make sure that the TypeScript compiler targets ES2021 or older
    // (see tsconfig.json)
    async click(): Promise<void> {
        this._textBox.value = 'Thinking hard...';
        this._button.setAttribute('disabled', "");

        await timeout(1000);

        incCounter();
        this._textBox.value = counter.toString();

        this._button.removeAttribute('disabled');
    }
}

new App();
