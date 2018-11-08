function bodyLoad() {
    document.getElementById('submit').innerHTML = "Change language";
}

function submitClick() {
    return confirm('You\'re about to change the language. Are you sure?');
}