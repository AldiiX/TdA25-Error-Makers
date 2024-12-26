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
        },

        setDifficultyIconStyle: function(game: any): any {
            const obj: any = {};
            if(game === null) return;

            switch(game?.difficulty) {
                case "beginner": obj["maskImage"] = "url(/images/icons/zarivka_beginner_bile.svg)"; break;
                case "easy": obj["maskImage"] = "url(/images/icons/zarivka_easy_bile.svg)"; break;
                case "medium": obj["maskImage"] = "url(/images/icons/zarivka_medium_bile.svg)"; break;
                case "hard": obj["maskImage"] = "url(/images/icons/zarivka_hard_bile.svg)"; break;
                case "extreme": obj["maskImage"] = "url(/images/icons/zarivka_extreme_bile.svg)"; break;
            }

            return obj;
        },

        howLongSinceLastUpdate(game: any): string {
            const updatedAt = new Date(game.updatedAt);
            const now = new Date();
            const diffMs = now.getTime() - updatedAt.getTime();

            const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24)); 
            const diffHours = Math.floor((diffMs % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60)); 
            const diffMinutes = Math.floor((diffMs % (1000 * 60 * 60)) / (1000 * 60)); 

            let timeAgo = '';
            if (diffDays > 0) {
                timeAgo = `${diffDays} ${this.getCzechDeclension(diffDays, 'den', 'dny', 'dní')} nazpět`;
            } else if (diffHours > 0) {
                timeAgo = `${diffHours} ${this.getCzechDeclension(diffHours, 'hodina', 'hodiny', 'hodin')} nazpět`;
            } else {
                timeAgo = `${diffMinutes} ${this.getCzechDeclension(diffMinutes, 'minuta', 'minuty', 'minut')} nazpět`;
            }
            return timeAgo;
        },

        getCzechDeclension(count: number, singular: string, few: string, plural: string): string {
            if (count === 1) {
                return singular;
            } else if (count >= 2 && count <= 4) {
                return few;
            } else {
                return plural;
            }
        }


    },

    computed: {
    },
})