export const vue = new Vue({
    el: "#app",
    mounted: function () {
        this.main();
    },
    data: {
        currentPlayer: null,
        game: {},
        gameLoaded: false,
        gameLocked: true,
        gameFadeOut: false,
        editMode: false,
    },
    methods: {
        main: function () {
            const _this = this;
            setTimeout(() => {
                this.getGame();
            }, 1150);
        },
        updateCell: function (_cell, index) {
            const cell = _cell;
            const _this = this;
            if (_this.gameLocked || cell.classList.contains("x") || cell.classList.contains("o"))
                return;
            cell.classList.add(_this.currentPlayer);
            _this.currentPlayer = _this.currentPlayer === "x" ? "o" : "x";
            _this.gameLocked = true;
            if (_this.game.isSaved)
                _this.cancelEditMode();
            const parent = document.querySelector(".mainsection .flex > .left .grid");
            const cells = parent.querySelectorAll(".cell");
            const board = Array.from({ length: 15 }, () => Array.from({ length: 15 }, () => ""));
            cells.forEach((cell, index) => {
                const x = Math.floor(index / 15);
                const y = index % 15;
                board[x][y] = cell.classList.contains("x") ? "X" : cell.classList.contains("o") ? "O" : "";
            });
            fetch(`/api/v1/games/${_this.game.uuid}/`, {
                method: "PUT",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({
                    board: board,
                    name: _this.game.name,
                    difficulty: _this.game.difficulty,
                })
            }).then(response => response.json()).then(data => {
                this.initializeGame(data);
            });
        },
        getGame: function () {
            const _this = this;
            const gameUUID = window.location.pathname.split("/")[2];
            fetch(`/api/v1/games/${gameUUID}`)
                .then(response => response.json())
                .then(data => {
                this.initializeGame(data);
            });
        },
        initializeGame: function (data) {
            const _this = this;
            _this.game = data;
            console.log(data);
            if (!_this.game.original)
                _this.game.original = {};
            _this.game.original.name = _this.game.name;
            _this.game.original.difficulty = _this.game.difficulty;
            const parent = document.querySelector(".mainsection .flex > .left .grid");
            const cells = parent.querySelectorAll(".cell");
            cells.forEach(cell => { cell.classList.remove("x", "o", "winning-cell"); });
            data.board.forEach((row, x) => {
                row.forEach((cell, y) => {
                    if (cell === "X") {
                        cells[x * 15 + y].classList.add("x");
                    }
                    else if (cell === "O") {
                        cells[x * 15 + y].classList.add("o");
                    }
                });
            });
            if (data.winningCells) {
                data.winningCells.forEach((cell) => {
                    cells[cell[0] * 15 + cell[1]]?.classList.add("winning-cell");
                });
            }
            const x = data.board.flat().filter((cell) => cell === "X").length;
            const o = data.board.flat().filter((cell) => cell === "O").length;
            _this.currentPlayer = x > o ? "o" : "x";
            _this.editMode = !data.isSaved;
            _this.gameLoaded = true;
            _this.gameLocked = false;
        },
        saveGame: function () {
            const _this = this;
            fetch(`/api/v1/games/${_this.game.uuid}/`, {
                method: "PUT",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({
                    board: _this.game.board,
                    name: document.getElementById("input-game-name")?.value ?? "Nová hra",
                    difficulty: document.getElementById("input-game-difficulty")?.value ?? "medium",
                    saved: true,
                })
            }).then(response => response.json()).then(data => {
                _this.editMode = false;
                this.getGame();
            });
        },
        deleteGame: function () {
            const _this = this;
            fetch(`/api/v1/games/${_this.game.uuid}/`, {
                method: "DELETE",
            }).then(response => {
                window.location.href = "/games";
            });
        },
        resetGame: function () {
            const _this = this;
            if (_this.gameLocked)
                return;
            _this.gameLocked = true;
            fetch(`/api/v1/games/${_this.game.uuid}/`, {
                method: "PATCH",
                headers: {
                    "Content-Type": "application/json"
                }
            }).then(response => response.json()).then(data => {
                this.initializeGame(data);
            });
        },
        cancelEditMode: function () {
            const _this = this;
            _this.editMode = false;
            _this.game.name = _this.game.original.name;
            _this.game.difficulty = _this.game.original.difficulty;
        },
        createNewGame: function () {
            const _this = this;
            _this.gameLocked = true;
            const preloadLink = document.createElement("link");
            preloadLink.href = "/game";
            preloadLink.rel = "prefetch";
            document.head.appendChild(preloadLink);
            const bgDiv = document.querySelector(".background-f55288d9-4dcf-456d-87c4-26be60c16cdb");
            const blurBgDiv = document.querySelector(".bg-29aa2e9f-d314-4366-a4cd-95ba0bbd1433");
            bgDiv.classList.add("fade-out");
            _this.gameFadeOut = true;
            blurBgDiv.classList.add("disableanimations");
            setTimeout(() => {
                fetch(`/api/v1/games/${_this.game.uuid}/`, {
                    method: "DELETE",
                }).then();
                window.location.href = "/game";
            }, 1500);
        },
        setPlayerColor: function () {
            const _this = this;
            if (_this.game?.winner?.toUpperCase() === "X")
                return "var(--accent-color-secondary)";
            if (_this.game?.winner?.toUpperCase() === "O")
                return "var(--accent-color-primary)";
            return _this.currentPlayer == 'x' ? 'var(--accent-color-secondary)' : 'var(--accent-color-primary)';
        },
    },
    computed: {},
});
