function CounterComponent() {
    return {
        data: function () {
            return {
                thinking: false,
                counter: 0
            }
        },
        computed: {
            status: function () {
                return this.thinking ? 'Thinking hard...' : this.counter;
            }
        },
        methods: {
            onClick: function () {
                this.thinking = true;

                self = this;
                setTimeout(function () {
                    self.counter++;
                    self.thinking = false;
                }, 1000);
            }
        }
    }
}