// @ts-ignore
import { scrollToElement } from "/scripts/functions.js";

// @ts-ignore
export const vue = new Vue({
    el: "#app",
    mounted: function(){
        this.main();
    },






    data: {
        games: [],
    },





    methods: {
        main: function(): void {
            const _this = this as any;

            fetch("/api/v1/games")
                .then(response => response.json())
                .then(data => {
                    _this.games = data;
                })
                .catch(error => {
                    console.error("Error:", error);
                }
            );
        },
    },

    computed: {
    },
})