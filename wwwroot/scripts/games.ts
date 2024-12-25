// @ts-ignore
import { scrollToElement } from "/scripts/functions.js";

// @ts-ignore
export const vue = new Vue({
    el: "#app",
    mounted: function(){
        this.main();
    },






    data: {
        temp: {
            filterText: "",
        },

        games: [],
        gamesFiltered:  [],
    },





    methods: {
        main: function(): void {
            const _this = this as any;

            fetch("/api/v1/games")
                .then(response => response.json())
                .then(data => {
                    _this.games = data;
                    _this.gamesFiltered = data;
                })
                .catch(error => {
                    console.error("Error:", error);
                }
            );
        },

        filterGames: function(prop: string): void {
            const _this = this as any;
            if(prop === "") {
                _this.gamesFiltered = _this.games;
                return;
            }

            _this.temp.filterText = prop;
            _this.gamesFiltered = _this.games?.filter((game: any) => {
                return game.name.toLowerCase().includes(prop.toLowerCase());
            });
        }
    },

    computed: {
    },
})