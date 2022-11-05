function App() {
    this.start = function () {
        new Vue({
            el: '#app',
            components: {
                'counter': CounterComponent()
            }
        });
    }
}
