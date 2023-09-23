export function sayHello(name) {
    alert("Welcome, " + name + "!");
}

const importUrl = new URL(import.meta.url);

// Top level awaits are also supported by the bundler.
// (Though the browser must support the async/await feature introduced in ES2017).
await new Promise(resolve => $(document).ready(resolve));

const name = importUrl.searchParams.get("hello");
if (name && window.autoSayHello) {
    sayHello(name);
}
