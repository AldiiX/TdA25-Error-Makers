// @ts-ignore
import { scrollToElement, openModal, deepClone } from "/scripts/functions.js";

// @ts-ignore
export const vue = new Vue({
    el: "#app",
    mounted: function(){
        this.main();
    },






    data: {
        temp: {
            filterText: "",
            filterDifficulty: "",
            filterStartDate: "",
            filterEndDate: ""
        },

        filterName: "",
        filterDifficulty: "",
        filterStartDate: "",  
        filterEndDate: "",    
        selectedDateRange: "",  

        games: null,
        gamesFiltered: [],
        modalOpened: null,
        editingGame: null,
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

        openModal: function (modalId: string|null): void {
            openModal(this, modalId);
        },

        resetFilters: function(): void {
            const _this = this as any;
            _this.filterName = "";
            _this.filterDifficulty = "";
            _this.filterStartDate = "";
            _this.filterEndDate = "";
            _this.selectedDateRange = "";

            this.filterGames("", "", "", "");
        },

        filterGames: function (name: string, difficulty: string, startDate: string, endDate: string): void {
            const _this = this as any;
    
            
            if (!name && !difficulty && !startDate && !endDate) {
                _this.gamesFiltered = _this.games;
                return;
            }

            _this.gamesFiltered = _this.games.filter((game: any) => {
                const matchesName = name
                    ? game.name?.toLowerCase().includes(name.toLowerCase())
                    : true;

                const matchesDifficulty = difficulty
                    ? game.difficulty?.toLowerCase() === difficulty.toLowerCase()
                    : true;

                const matchesDate = _this.filterByDate(game.updatedAt, startDate, endDate);

                return matchesName && matchesDifficulty && matchesDate;
            });
        },

        filterByDate: function(gameDate: string, startDate: string, endDate: string): boolean {
            const gameUpdated = new Date(gameDate);
            
            if (!startDate && !endDate) {
                return true;
            }

            const start = new Date(startDate);
            start.setHours(0, 0, 0, 0);

            const end = new Date(endDate);
            end.setHours(23, 59, 59, 999);
            
            if (startDate && endDate) {
                return gameUpdated >= start && gameUpdated <= end;
            }

            return true; 
        },

        updateFilters: function(): void {
            const _this = this as any;
            _this.filterGames(_this.filterName, _this.filterDifficulty, _this.filterStartDate, _this.filterEndDate);
        },

        setDateRange: function(range: string): void {
            const _this = this as any;
            const now = new Date();
            let startDate = new Date();
            startDate.setHours(0, 0, 0, 0);

            let endDate = new Date();
            endDate.setHours(23, 59, 59, 999);

            switch (range) {
                case 'today':
                    startDate = now;
                    break;
                case 'last7days':
                    startDate.setDate(now.getDate() - 7);
                    break;
                case 'last30days':
                    startDate.setDate(now.getDate() - 30);
                    break;
                case 'lastYear':
                    startDate.setFullYear(now.getFullYear() - 1);
                    break;
                case 'custom':
                    
                    break;
                case '':  
                    _this.filterStartDate = "";
                    _this.filterEndDate = "";
                    break;
                default:
                    startDate = now;
                    break;
            }
            
            if (range !== 'custom' && range !== '') {
                _this.filterStartDate = startDate.toISOString().split('T')[0];
                _this.filterEndDate = endDate.toISOString().split('T')[0];
            }

            _this.selectedDateRange = range;
            _this.updateFilters();
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

        setGameDifficultyText: function(game: any): string {
            switch(game?.difficulty) {
                case "beginner": return "Začátečník";
                case "easy": return "Lehká";
                case "medium": return "Střední";
                case "hard": return "Těžká";
                case "extreme": return "Extrémní";
            }

            return "Neznámá";
        },

        setGameStateText: function(game: any): string {
            switch(game?.gameState) {
                case "opening": return "Zahájení";
                case "midgame": return "V průběhu";
                case "endgame": return "Koncovka";
                case "finished": return "Dohraná";
            }

            return "Neznámý";
        },

        howLongSinceLastUpdate(game: any): string {
            const updatedAt = new Date(game.updatedAt);
            const now = new Date();
            const diffMs = now.getTime() - updatedAt.getTime();

            const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));
            const diffHours = Math.floor((diffMs % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
            const diffMinutes = Math.floor((diffMs % (1000 * 60 * 60)) / (1000 * 60));
            const diffSeconds = Math.floor((diffMs % (1000 * 60)) / 1000);

            let timeAgo = '';
            if (diffDays > 0) {
                timeAgo = `${diffDays} ${this.getCzechDeclension(diffDays, 'den', 'dny', 'dní')} nazpět`;
            } else if (diffHours > 0) {
                timeAgo = `${diffHours} ${this.getCzechDeclension(diffHours, 'hodina', 'hodiny', 'hodin')} nazpět`;
            } else if (diffMinutes > 0) {
                timeAgo = `${diffMinutes} ${this.getCzechDeclension(diffMinutes, 'minuta', 'minuty', 'minut')} nazpět`;
            } else {
                timeAgo = `před pár sekundama`;
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
        },

        deepClone: function(obj: any): any {
            return deepClone(obj);
        },

        saveEditedGame: function(game: any|null) {
            const _this = this as any;
            game ??= _this.editingGame;
            this.openModal(null);

            fetch(`/api/v1/games/${game.uuid}`, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ name: game.name, difficulty: game.difficulty, board: game.board }),
            }).then(async response => {
                const data = await response.json();

                _this.games.forEach((g: any, index: number) => {
                    if (g.uuid === game.uuid) {
                        _this.games[index] = data;
                    }
                });

                _this.filterGames(_this.filterName, _this.filterDifficulty, _this.filterStartDate, _this.filterEndDate);
            });
        },
    },

    computed: {
    },
})