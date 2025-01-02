// @ts-ignore
import { scrollToElement } from "/scripts/functions.js";

// @ts-ignore
export const vue = new Vue({
    el: "#app",
    mounted: function(){
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
        main: function(): void {
            const _this = this as any;

            setTimeout(() => { // fake loading jen tak for fun aby to vypadalo pěkně
                this.getGame();
            }, 1150);
        },

        updateCell: function(_cell: any, index: number): void {
            const cell = _cell as HTMLElement;
            const _this = this as any;
            if(_this.gameLocked || cell.classList.contains("x") || cell.classList.contains("o")) return;

            cell.classList.add(_this.currentPlayer);
            _this.currentPlayer = _this.currentPlayer === "x" ? "o" : "x";
            _this.gameLocked = true;
            if(_this.game.isSaved) _this.cancelEditMode();



            // vytvoření dvourozměrného pole z div elemtu
            const parent = document.querySelector(".mainsection .flex > .left .grid") as HTMLElement;
            const cells = parent.querySelectorAll(".cell");
            const board = Array.from({ length: 15 }, () => Array.from({ length: 15 }, () => ""));

            cells.forEach((cell, index) => {
                const x = Math.floor(index / 15);
                const y = index % 15;

                board[x][y] = cell.classList.contains("x") ? "X" : cell.classList.contains("o") ? "O" : "";
            });



            fetch(`/api/v2/games/${_this.game.uuid}/`, {
                method: "PUT",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({
                    board: board,
                    name: _this.game.name,
                    difficulty: _this.game.difficulty,
                })
            }).then(async response => {
                const data = await response.json();
                if(!response.ok) throw new Error();

                this.initializeGame(data);
            }).catch(_ => {
                this.getGame();
            });
        },

        getGame: function(): void {
            const _this = this as any;
            const gameUUID = window.location.pathname.split("/")[2];

            fetch(`/api/v2/games/${gameUUID}`)
                .then(async response => {
                    const data = await response.json();
                    if(!response.ok) throw new Error();

                    this.initializeGame(data);
                }
            ).catch(error => {
                location.href = `/error?code=404&message=Chyba při spouštění hry&buttonLink=/games`;
            });
        },

        initializeGame: function(data: any): void {
            const _this = this as any;
            if(data === null) throw new Error("Data is null");


            _this.game = data;
            //console.log(data);
            window.scroll({top: 0, left: 0, behavior: "smooth"});

            if (!_this.game.original) _this.game.original = {};
            _this.game.original.name = _this.game.name;
            _this.game.original.difficulty = _this.game.difficulty;

            // vyrenderování boardy
            const parent = document.querySelector(".mainsection .flex > .left .grid") as HTMLElement;
            const cells = parent.querySelectorAll(".cell");
            cells.forEach(cell => { cell.classList.remove("x", "o", "winning-cell"); });

            data.board.forEach((row: any, x: number) => {
                row.forEach((cell: any, y: number) => {
                    if (cell === "X") {
                        cells[x * 15 + y].classList.add("x");
                    } else if (cell === "O") {
                        cells[x * 15 + y].classList.add("o");
                    }
                });
            });

            // podle data.winningcells vykreslit výherní buňky
            if(data.winningCells) {
                data.winningCells.forEach((cell: any) => {
                    cells[cell[0] * 15 + cell[1]]?.classList.add("winning-cell");
                });
            }


            // zjištění, kdo je na tahu
            const x = data.board.flat().filter((cell: any) => cell === "X").length;
            const o = data.board.flat().filter((cell: any) => cell === "O").length;

            _this.currentPlayer = x > o ? "o" : "x";
            _this.editMode = !data.isSaved;
            _this.gameLoaded = true;
            _this.gameLocked = false;

            // nastavení titlu
            if(String(data.name).toLowerCase().includes("hra")) document.title = `${data.name} • Think Different Academy`;
            else document.title = `${data.name} - Hra • Think Different Academy`;
        },

        saveGame: function(): void {
            const _this = this as any;

            fetch(`/api/v2/games/${_this.game.uuid}/`, {
                method: "PUT",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({
                    board: _this.game.board,
                    name: (document.getElementById("input-game-name") as HTMLInputElement)?.value ?? "Nová hra",
                    difficulty: (document.getElementById("input-game-difficulty") as HTMLInputElement)?.value ?? "medium",
                    saved: true,
                })
            }).then(async response => {
                const data = await response.json();
                if(!response.ok) throw new Error();

                this.initializeGame(data);
            }).catch(_ => {
                this.getGame();
            });
        },

        deleteGame: function(): void {
            const _this = this as any;

            fetch(`/api/v2/games/${_this.game.uuid}/`, {
                method: "DELETE",
            }).then(response => {
                window.location.href = "/games";
            });
        },

        resetGame: function (): void {
            const _this = this as any;
            if(_this.gameLocked) return;

            _this.gameLocked = true;
            fetch(`/api/v2/games/${_this.game.uuid}/`, {
                method: "PATCH",
                headers: {
                    "Content-Type": "application/json"
                }
            }).then(async response => {
                const data = await response.json();
                if(!response.ok) throw new Error();

                this.initializeGame(data);
            }).catch(_ => {
                this.getGame();
            });
        },

        cancelEditMode: function() {
            const _this = this as any;
            _this.editMode = false;
            _this.game.name = _this.game.original.name;
            _this.game.difficulty = _this.game.original.difficulty;
        },

        createNewGame: function(): void {
            const _this = this as any;
            _this.gameLocked = true;

            // preload /game stránku
            const preloadLink = document.createElement("link");
            preloadLink.href = "/game";
            preloadLink.rel = "prefetch";
            //preloadLink.as = "document";
            document.head.appendChild(preloadLink);


            const bgDiv = document.querySelector(".background-f55288d9-4dcf-456d-87c4-26be60c16cdb") as HTMLElement;
            const newGameHeaderButton = document.querySelector(".header-5F015D44-0984-4A50-B52B-5319AE57C19C > .flex > .Login .newgame") as HTMLElement;
            const blurBgDiv = document.querySelector(".bg-29aa2e9f-d314-4366-a4cd-95ba0bbd1433") as HTMLElement;
            bgDiv.classList.add("fade-out");
            _this.gameFadeOut = true;
            blurBgDiv.classList.add("disableanimations");
            newGameHeaderButton.style.pointerEvents = "none";

            setTimeout(() => {
                fetch(`/api/v2/games/${_this.game.uuid}/`, {
                    method: "DELETE",
                }).then();

                window.location.href = "/game";
            }, 1500);
        },

        setPlayerColor: function (): any {
            const _this = this as any;
            if(_this.game?.winner?.toUpperCase() === "X") return "var(--accent-color-secondary)";
            if(_this.game?.winner?.toUpperCase() === "O") return "var(--accent-color-primary)";

            return _this.currentPlayer == 'x' ? 'var(--accent-color-secondary)' : 'var(--accent-color-primary)'
        },
    },

    computed: {
    },
})