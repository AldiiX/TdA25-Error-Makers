// @ts-ignore
import { scrollToElement, openModal, deepClone, enableScroll, disableScroll, addAnnouncement } from "/scripts/functions.js";

// @ts-ignore
export const vue = new Vue({
    el: "#app",
    mounted: function(){
        this.main();


        document.addEventListener("keydown", (e) => {
            if (e.key === "Escape") {
                if (this.modalOpened === "editgame") {
                    this.openModal(null);
                }
            }
        });
    },






    data: {
        temp: {
            filterText: "",
            filterDifficulty: "",
            filterStartDate: "",
            filterEndDate: "",
            creatingNewGame: false,
            editingGameIsInvalid: false,
            editingGameError: null,
            loadingGame: false,
        },

        filterName: "",
        filterDifficulty: "",
        filterStartDate: "",  
        filterEndDate: "",    
        selectedDateRange: "",
        menuExpanded: false,

        games: null,
        fillerGames: 0,
        gamesFiltered: [],
        gamesFilteredOnPage: [],
        currentPage: 1,
        maxGamesPerPage: 15,


        modalOpened: null,
        editingGame: null,
        announcements: [],
    },






    methods: {
        main: function(): void {
            const _this = this as any;

            this.fetchGamesFromAPI();
        },
        
        fetchGamesFromAPI: function (): void {
            const _this = this as any;

            fetch("/api/v2/games")
                .then(response => response.json())
                .then(data => {
                    _this.games = data;
                    _this.filterGames();
                })
                .catch(error => {
                        console.error("Error:", error);
                    }
                );
        },

        openModal: function (modalId: string|null): void {
            const _this = this as any;


            // před zavřením modalu
            setTimeout(() => {
                if(_this.modalOpened === "editgame" && modalId === null) {
                    const board = document.querySelector(".modal-editgame > .modal > .right > .grid") as HTMLElement;
                    const cells = board?.querySelectorAll(".cell");
                    cells?.forEach(cell => { cell.classList.remove("x", "o", "winning-cell"); });
                    _this.temp.creatingNewGame = false;
                    _this.temp.editingGameIsInvalid = false;
                    _this.temp.editingGameError = null;
                }
            }, 300);


            openModal(this, modalId);


            // po otevření modalu
            setTimeout(() => {
                if (modalId === "editgame") {
                    //console.warn(_this.editingGame);
                    this.renderBoard(_this.editingGame);
                }
            }, 300);
        },

        addAnnouncement: function(text: string, type = 'info', timeout = 5000): void {
            addAnnouncement(this, text, type, timeout);
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

        updateFillerGames: function (): void {
            const _this = this as any;

            _this.fillerGames = _this.gamesFilteredOnPage.length % 3 !== 0 ? 3 - (_this.gamesFilteredOnPage.length % 3) : 0;
        },

        filterGames: function (name: string, difficulty: string, startDate: string, endDate: string): void {
            const _this = this as any;
            _this.gamesFiltered = _this.games;

            if (!name && !difficulty && !startDate && !endDate) {
                _this.gamesFiltered = _this.games;
                _this.setPage(1);
                _this.updateFillerGames();
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

            // nastavení fillergames na počet, aby vždycky číslo her bylo dělitelné 3
            _this.setPage(1);
        },

        setPage: function(page: number): void {
            const _this = this as any;
            _this.currentPage = page;
            const start = (page - 1) * _this.maxGamesPerPage;
            const end = start + _this.maxGamesPerPage;
            _this.gamesFilteredOnPage = _this.gamesFiltered.slice(start, end);
            _this.updateFillerGames();
        },

        getMaxPage: function(): number {
            const _this = this as any;
            return Math.ceil(_this.gamesFiltered.length / _this.maxGamesPerPage);
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

        areFiltersEmpty: function(): boolean {
            const _this = this as any;
            return !_this.filterName && !_this.filterDifficulty && !_this.filterStartDate && !_this.filterEndDate;
        },

        scrollToElement: function(element: string): void {
            scrollToElement(element);
        },

        setGameDifficultyText: function(game: any): string {
            switch(game?.difficulty) {
                case "beginner": return "Začátečník";
                case "easy": return "Lehká";
                case "medium": return "Střední";
                case "hard": return "Těžká";
                case "extreme": return "Extrémně těžká";
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


            // zpracování jména a obtížnosti
            const name: string = _this.editingGame.name;
            const difficulty: string = _this.editingGame.difficulty;
            if(!name) {
                _this.temp.editingGameError = "Název hry nemůže být prázdný.";
                _this.addAnnouncement("Chyba ukládání hry: Název hry nemůže být prázdný.", 'error', 2000);
                return;
            }

            if(!difficulty) {
                _this.temp.editingGameError = "Obtížnost hry nemůže být prázdná.";
                _this.addAnnouncement("Chyba ukládání hry: Obtížnost hry nemůže být prázdná.", 'error', 2000);
                return;
            }



            // zpracování board
            const board = document.querySelector(".modal-editgame > .modal > .right > .grid") as HTMLElement;
            const cells = board.querySelectorAll(".cell");
            game.board = [];
            for (let i = 0; i < 15; i++) {
                const row = [];
                for (let j = 0; j < 15; j++) {
                    row.push(cells[i * 15 + j].classList.contains("x") ? "X" : cells[i * 15 + j].classList.contains("o") ? "O" : "");
                }

                game.board.push(row);
            }



            // checknutí zda je boarda prázdná
            let empty: boolean = true;
            game.board.forEach((row: any) => {
                row.forEach((cell: any) => {
                    if(cell !== "") empty = false;
                });
            });

            if(empty) {
                _this.temp.editingGameError = "Herní plocha nemůže být prázdná.";
                _this.addAnnouncement("Chyba ukládání hry: Herní plocha nemůže být prázdná.", 'error', 2000);
                return;
            }



            // request
            if(!_this.temp.creatingNewGame) { // editace hry
                fetch(`/api/v2/games/${game.uuid}`, {
                    method: 'PUT',
                    headers: {
                        'Content-Type': 'application/json',
                    },
                    body: JSON.stringify({
                        name: game.name,
                        difficulty: game.difficulty,
                        board: game.board,
                        saved: true,
                        saveIfFinished: true,
                        errorIfSavingCompleted: true,
                    }),
                }).then(async response => {
                    const data = await response.json();
                    if (!response.ok) {
                        console.error("Error: ", data.message);
                        _this.temp.editingGameError = data.message;
                        _this.addAnnouncement("Chyba při ukládání hry: " + data.message, 'error', 2000);
                        return;
                    }

                    _this.games.forEach((g: any, index: number) => {
                        if (g.uuid === game.uuid) {
                            _this.games[index] = data;
                        }
                    });

                    _this.resetFilters();
                    _this.addAnnouncement(`Hra „${name}” byla úspěšně upravena.`, "info", 4000);
                    this.openModal(null);
                });
            }
            else { // vytvoření hry
                fetch(`/api/v2/games`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                    },
                    body: JSON.stringify({
                        name: game.name,
                        difficulty: game.difficulty,
                        board: game.board,
                        saved: true,
                        errorIfSavingCompleted: true,
                    }),
                }).then(async response => {
                    const data = await response.json();
                    if (!response.ok) {
                        console.error("Error: ", data.message);
                        _this.temp.editingGameError = data.message;
                        return;
                    }

                    fetch("/api/v2/games")
                        .then(async response => {
                            const data = await response.json();
                            if (!response.ok) {
                                console.error("Error: ", data.message);
                                _this.addAnnouncement("Chyba při vytvoření hry: " + data.message, 'error', 2000);
                                return;
                            }

                            _this.games = data;
                            _this.gamesFiltered = data;
                            _this.resetFilters();
                            _this.addAnnouncement(`Hra „${name}” byla úspěšně vytvořena.`, "info", 4000);
                            this.openModal(null);
                        })
                        .catch(error => {
                                console.error("Error:", error);
                                _this.addAnnouncement("Chyba při vytvoření hry: " + error, 'error', 2000);
                            }
                        );
                });
            }
        },

        renderBoard: function (data: any) {
            const _this = this as any;
            const board = data.board;

            // vyrenderování boardy
            const parent = document.querySelector(".modal-editgame > .modal > .right > .grid") as HTMLElement;
            const cells = parent.querySelectorAll(".cell");
            cells.forEach(cell => { cell.classList.remove("x", "o", "winning-cell"); });

            board.forEach((row: any, x: number) => {
                row.forEach((cell: any, y: number) => {
                    if (cell === "X") {
                        cells[x * 15 + y].classList.add("x");
                    } else if (cell === "O") {
                        cells[x * 15 + y].classList.add("o");
                    }
                });
            });

            // podle data.winningcells vykreslit výherní buňky
            /*if(data.winningCells) {
                data.winningCells.forEach((cell: any) => {
                    cells[cell[0] * 15 + cell[1]]?.classList.add("winning-cell");
                });
            }*/

            // checknutí validity
            _this.checkBoardValidity();
        },

        updateCell: function(_cell: any, index: number): void {
            const cell = _cell as HTMLElement;
            const _this = this as any;

            if(!cell.classList.contains("x") && !cell.classList.contains("o")) {
                cell.classList.add("x");
            }

            else if(cell.classList.contains("o")) {
                cell.classList.remove("o");
            }

            else if(cell.classList.contains("x")) {
                cell.classList.remove("x");
                cell.classList.add("o");
            }

            _this.checkBoardValidity();
        },

        updateCellRightClick: function(_cell: any, index: number): void {
            const cell = _cell as HTMLElement;
            const _this = this as any;

            cell.classList.remove("x", "o");

            _this.checkBoardValidity();
        },

        checkBoardValidity: function(): void {
            const _this = this as any;

            // kontrola zda je počet křížků a koleček vyvážený
            const cells = document.querySelectorAll(".modal-editgame > .modal > .right > .grid .cell");
            let x = 0;
            let o = 0;
            cells.forEach(cell => {
                if(cell.classList.contains("x")) x++;
                if(cell.classList.contains("o")) o++;
            });

            if(x === o) {
                _this.temp.editingGameIsInvalid = false;
            } else if(x === o +1) {
                _this.temp.editingGameIsInvalid = false;
            } else _this.temp.editingGameIsInvalid = true;
        },

        createNewGame: async function(): Promise<void> {
            const _this = this as any;
            const randomName = (await fetch("/api/v2/games/generate-name").then(response => response.json())).name;

            _this.editingGame = {
                name: randomName,
                difficulty: "medium",
            }

            _this.temp.creatingNewGame = true;
            openModal(_this, "editgame");
        },

        setEditGameModalStyle: function (): any {
            const _this = this as any;
            const obj: any = {};
            const game = _this.editingGame;

            if (_this.temp.editingGameIsInvalid) {
                obj["border"] = "1px solid var(--accent-color-secondary)";
            }

            return obj;
        },

        playGame: function(game: any) {
            const _this = this as any;
            _this.temp.loadingGame = true;

            fetch("/api/v2/games", {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    name: game.name,
                    difficulty: game.difficulty,
                    board: game.board,
                    saved: false,
                    isInstance: true,
                }),
            }).then(async response => {
                const data = await response.json();
                if (!response.ok) {
                    console.error("Chyba spuštění hry: ", data.message);
                    _this.addAnnouncement("Chyba spuštění hry: " + data.message, 'error', 2000);
                    _this.temp.loadingGame = false;
                    return;
                }

                window.location.href = `/game/${data.uuid}`;
            })
        },

        deleteGame: function (game: any|null = null): void {
            const _this = this as any;
            game ??= _this.editingGame;
            const name = game.name;

            fetch(`/api/v2/games/${game.uuid}`, {
                method: 'DELETE',
            }).then(async response => {
                if (!response.ok) {
                    const data = await response.json();
                    console.error("Error: ", data.message);
                    return;
                }

                _this.games = _this.games.filter((g: any) => g.uuid !== game.uuid);
                _this.gamesFiltered = _this.games;
                _this.resetFilters();
                this.openModal(null);
                _this.addAnnouncement(`Hra „${name}” byla úspěšně smazána.`, "info", 4000);
            });
        }
    },

    computed: {
    },
})