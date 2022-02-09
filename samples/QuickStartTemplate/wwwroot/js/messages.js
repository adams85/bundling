export function sayHello(name) {
    alert("Welcome, " + name + "!");
}

var importUrl = new URL(import.meta.url);

var name = importUrl.searchParams.get("hello");
if (name) {
    $(document).ready(function () {
        if (window.autoSayHello) {
            sayHello(name);
        }
    });
}
