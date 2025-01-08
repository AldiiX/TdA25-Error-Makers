import { openModal, deepClone } from "/scripts/functions.js";
export const vue = new Vue({
    el: "#app",
    mounted: function () {
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
        games: null,
        gamesFiltered: [],
        modalOpened: null,
        editingGame: null,
    },
    methods: {
        main: function () {
            const _this = this;
            this.fetchGamesFromAPI();
        },
        fetchGamesFromAPI: function () {
            const _this = this;
            fetch("/api/v2/games")
                .then(response => response.json())
                .then(data => {
                _this.games = data;
                _this.gamesFiltered = data;
            })
                .catch(error => {
                console.error("Error:", error);
            });
        },
        openModal: function (modalId) {
            const _this = this;
            setTimeout(() => {
                if (_this.modalOpened === "editgame" && modalId === null) {
                    const board = document.querySelector(".modal-editgame > .modal > .right > .grid");
                    const cells = board?.querySelectorAll(".cell");
                    cells?.forEach(cell => { cell.classList.remove("x", "o", "winning-cell"); });
                    _this.temp.creatingNewGame = false;
                    _this.temp.editingGameIsInvalid = false;
                    _this.temp.editingGameError = null;
                }
            }, 300);
            openModal(this, modalId);
            setTimeout(() => {
                if (modalId === "editgame") {
                    this.renderBoard(_this.editingGame);
                }
            }, 300);
        },
        resetFilters: function () {
            const _this = this;
            _this.filterName = "";
            _this.filterDifficulty = "";
            _this.filterStartDate = "";
            _this.filterEndDate = "";
            _this.selectedDateRange = "";
            this.filterGames("", "", "", "");
        },
        filterGames: function (name, difficulty, startDate, endDate) {
            const _this = this;
            if (!name && !difficulty && !startDate && !endDate) {
                _this.gamesFiltered = _this.games;
                return;
            }
            _this.gamesFiltered = _this.games.filter((game) => {
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
        filterByDate: function (gameDate, startDate, endDate) {
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
        updateFilters: function () {
            const _this = this;
            _this.filterGames(_this.filterName, _this.filterDifficulty, _this.filterStartDate, _this.filterEndDate);
        },
        setDateRange: function (range) {
            const _this = this;
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
        setDifficultyIconStyle: function (game) {
            const obj = {};
            if (game === null)
                return;
            switch (game?.difficulty) {
                case "beginner":
                    obj["maskImage"] = "url(/images/icons/zarivka_beginner_bile.svg)";
                    break;
                case "easy":
                    obj["maskImage"] = "url(/images/icons/zarivka_easy_bile.svg)";
                    break;
                case "medium":
                    obj["maskImage"] = "url(/images/icons/zarivka_medium_bile.svg)";
                    break;
                case "hard":
                    obj["maskImage"] = "url(/images/icons/zarivka_hard_bile.svg)";
                    break;
                case "extreme":
                    obj["maskImage"] = "url(/images/icons/zarivka_extreme_bile.svg)";
                    break;
            }
            return obj;
        },
        setGameDifficultyText: function (game) {
            switch (game?.difficulty) {
                case "beginner": return "Začátečník";
                case "easy": return "Lehká";
                case "medium": return "Střední";
                case "hard": return "Těžká";
                case "extreme": return "Extrémní";
            }
            return "Neznámá";
        },
        setGameStateText: function (game) {
            switch (game?.gameState) {
                case "opening": return "Zahájení";
                case "midgame": return "V průběhu";
                case "endgame": return "Koncovka";
                case "finished": return "Dohraná";
            }
            return "Neznámý";
        },
        howLongSinceLastUpdate(game) {
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
            }
            else if (diffHours > 0) {
                timeAgo = `${diffHours} ${this.getCzechDeclension(diffHours, 'hodina', 'hodiny', 'hodin')} nazpět`;
            }
            else if (diffMinutes > 0) {
                timeAgo = `${diffMinutes} ${this.getCzechDeclension(diffMinutes, 'minuta', 'minuty', 'minut')} nazpět`;
            }
            else {
                timeAgo = `před pár sekundama`;
            }
            return timeAgo;
        },
        getCzechDeclension(count, singular, few, plural) {
            if (count === 1) {
                return singular;
            }
            else if (count >= 2 && count <= 4) {
                return few;
            }
            else {
                return plural;
            }
        },
        deepClone: function (obj) {
            return deepClone(obj);
        },
        saveEditedGame: function (game) {
            const _this = this;
            game ??= _this.editingGame;
            const name = _this.editingGame.name;
            const difficulty = _this.editingGame.difficulty;
            if (!name) {
                _this.temp.editingGameError = "Název hry nemůže být prázdný.";
                return;
            }
            if (!difficulty) {
                _this.temp.editingGameError = "Obtížnost hry nemůže být prázdná.";
                return;
            }
            const board = document.querySelector(".modal-editgame > .modal > .right > .grid");
            const cells = board.querySelectorAll(".cell");
            game.board = [];
            for (let i = 0; i < 15; i++) {
                const row = [];
                for (let j = 0; j < 15; j++) {
                    row.push(cells[i * 15 + j].classList.contains("x") ? "X" : cells[i * 15 + j].classList.contains("o") ? "O" : "");
                }
                game.board.push(row);
            }
            let empty = true;
            game.board.forEach((row) => {
                row.forEach((cell) => {
                    if (cell !== "")
                        empty = false;
                });
            });
            if (empty) {
                _this.temp.editingGameError = "Hra nemůže být prázdná.";
                return;
            }
            if (!_this.temp.creatingNewGame) {
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
                }).then(async (response) => {
                    const data = await response.json();
                    if (!response.ok) {
                        console.error("Error: ", data.message);
                        _this.temp.editingGameError = data.message;
                        return;
                    }
                    _this.games.forEach((g, index) => {
                        if (g.uuid === game.uuid) {
                            _this.games[index] = data;
                        }
                    });
                    _this.resetFilters();
                    this.openModal(null);
                });
            }
            else {
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
                }).then(async (response) => {
                    const data = await response.json();
                    if (!response.ok) {
                        console.error("Error: ", data.message);
                        _this.temp.editingGameError = data.message;
                        return;
                    }
                    fetch("/api/v2/games")
                        .then(response => response.json())
                        .then(data => {
                        _this.games = data;
                        _this.gamesFiltered = data;
                        _this.resetFilters();
                        this.openModal(null);
                    })
                        .catch(error => {
                        console.error("Error:", error);
                    });
                });
            }
        },
        renderBoard: function (data) {
            const _this = this;
            const board = data.board;
            const parent = document.querySelector(".modal-editgame > .modal > .right > .grid");
            const cells = parent.querySelectorAll(".cell");
            cells.forEach(cell => { cell.classList.remove("x", "o", "winning-cell"); });
            board.forEach((row, x) => {
                row.forEach((cell, y) => {
                    if (cell === "X") {
                        cells[x * 15 + y].classList.add("x");
                    }
                    else if (cell === "O") {
                        cells[x * 15 + y].classList.add("o");
                    }
                });
            });
            _this.checkBoardValidity();
        },
        updateCell: function (_cell, index) {
            const cell = _cell;
            const _this = this;
            if (!cell.classList.contains("x") && !cell.classList.contains("o")) {
                cell.classList.add("x");
            }
            else if (cell.classList.contains("o")) {
                cell.classList.remove("o");
            }
            else if (cell.classList.contains("x")) {
                cell.classList.remove("x");
                cell.classList.add("o");
            }
            _this.checkBoardValidity();
        },
        updateCellRightClick: function (_cell, index) {
            const cell = _cell;
            const _this = this;
            cell.classList.remove("x", "o");
            _this.checkBoardValidity();
        },
        checkBoardValidity: function () {
            const _this = this;
            const cells = document.querySelectorAll(".modal-editgame > .modal > .right > .grid .cell");
            let x = 0;
            let o = 0;
            cells.forEach(cell => {
                if (cell.classList.contains("x"))
                    x++;
                if (cell.classList.contains("o"))
                    o++;
            });
            if (x === o) {
                _this.temp.editingGameIsInvalid = false;
            }
            else if (x === o + 1) {
                _this.temp.editingGameIsInvalid = false;
            }
            else
                _this.temp.editingGameIsInvalid = true;
        },
        createNewGame: async function () {
            const _this = this;
            const randomName = (await fetch("/api/v2/games/generate-name").then(response => response.json())).name;
            _this.editingGame = {
                name: randomName,
                difficulty: "medium",
            };
            _this.temp.creatingNewGame = true;
            openModal(_this, "editgame");
        },
        setEditGameModalStyle: function () {
            const _this = this;
            const obj = {};
            const game = _this.editingGame;
            if (_this.temp.editingGameIsInvalid) {
                obj["border"] = "1px solid var(--accent-color-secondary)";
            }
            return obj;
        },
        playGame: function (game) {
            const _this = this;
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
            }).then(async (response) => {
                const data = await response.json();
                if (!response.ok) {
                    console.error("Error: ", data.message);
                    _this.temp.loadingGame = false;
                    return;
                }
                window.location.href = `/game/${data.uuid}`;
            });
        },
        deleteGame: function (game = null) {
            const _this = this;
            game ??= _this.editingGame;
            fetch(`/api/v2/games/${game.uuid}`, {
                method: 'DELETE',
            }).then(async (response) => {
                if (!response.ok) {
                    const data = await response.json();
                    console.error("Error: ", data.message);
                    return;
                }
                _this.games = _this.games.filter((g) => g.uuid !== game.uuid);
                _this.gamesFiltered = _this.games;
                _this.resetFilters();
                this.openModal(null);
            });
        }
    },
    computed: {},
});
