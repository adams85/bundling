// http://exploringjs.com/es6/ch_modules.html#_imports-are-read-only-views-on-exports
export let counter: number = 0;

export function incCounter(): void {
    counter++;
}
