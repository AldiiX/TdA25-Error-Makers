export const vue = new Vue({
    el: "#app",
    mounted: function () {
        this.main();
    },
    data: {
        temp: {
            filterText: "",
        },
        games: [],
        gamesFiltered: [],
    },
    methods: {
        main: function () {
            const _this = this;
            fetch("/api/v1/games")
                .then(response => response.json())
                .then(data => {
                _this.games = data;
                _this.gamesFiltered = data;
            })
                .catch(error => {
                console.error("Error:", error);
            });
        },
        filterGames: function (prop) {
            const _this = this;
            if (prop === "") {
                _this.gamesFiltered = _this.games;
                return;
            }
            _this.temp.filterText = prop;
            _this.gamesFiltered = _this.games?.filter((game) => {
                return game.name.toLowerCase().includes(prop.toLowerCase());
            });
        }
    },
    computed: {},
});
