export const vue = new Vue({
    el: "#app",
    mounted: function () {
        this.main();
    },
    data: {
        games: [],
    },
    methods: {
        main: function () {
            const _this = this;
            fetch("/api/v1/games")
                .then(response => response.json())
                .then(data => {
                _this.games = data;
            })
                .catch(error => {
                console.error("Error:", error);
            });
        },
    },
    computed: {},
});
