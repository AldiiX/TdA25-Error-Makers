// @ts-ignore
import { scrollToElement, getCookie, addAnnouncement } from "/scripts/functions.js";

// @ts-ignore
export const vue = new Vue({
    el: "#app",
    mounted: function(){
        const _this = this as any;
        this.main();

        window.addEventListener("scroll", () => {
            _this.pageIsScrolled = window.scrollY > 0;
        });
    },






    data: {
        currentPlayer: null,
        game: null,
        gameLoaded: false,
        gameNumberOfPlayers: 0,
        gameLocked: true,
        gameFadeOut: false,
        editMode: false,
        pageIsScrolled: false,
        menuExpanded: false,
        accountUUID: null,
        accountName: null,
        announcements: [],
        socket: null,
        chatMessages: [],
        chatMessageInput: "",
        gameFazeIsEnding: false,
    },





    methods: {
        main: function(): void {
            const _this = this as any;


            //_this.gameLoaded = true;
            this.connectToSocket();
        },

        connectToSocket: function(): void {
            const _this = this as any;
            const uuid = window.location.pathname.split("/")[2];
            const locationIsLocalhost = window.location.host.includes("localhost");
            const socket = new WebSocket(`${locationIsLocalhost ? "ws" : "wss"}://${window.location.host}/ws/multiplayer/game/${uuid}`);
            _this.socket = socket;

            socket.onopen = () => {
                console.log("Connected to the server");
            }

            socket.onmessage = (event) => {
                _this.receiveSocketMessage(event);
            }
        },

        receiveSocketMessage: function(event: MessageEvent<any>) {
            const _this = this as any;
            const data = JSON.parse(event.data);
            console.log(data);

            if(data.c === "UNA1") location.href="/error?code=404&message=Hra skončila&buttonLink=/play";

            if(data.action === "status") {
                _this.gameNumberOfPlayers = data.playerCount;
            }

            if(data.action === "updateGame") {
                this.initializeGame(data.game);
            }

            if(data.action === "finishGame") {
                _this.socket.close();
                _this.socket = null;
            }

            if(data.action === "chatMessage") {
                (_this.chatMessages as any[]).push({
                    sender: data.sender,
                    message: data.message,
                    letter: String(data.letter).toUpperCase(),
                    isMe: data.sender === _this.accountName,
                });

                // scrollnuti chatu dolu
                setTimeout(() => {
                    const chatDiv = document.querySelector(".chat-messages") as HTMLElement;
                    chatDiv.scroll({top: chatDiv.scrollHeight + 100000, behavior: 'smooth'});
                }, 10);
            }




            if(!_this.gameLoaded && data.action === "status" && data.playerCount >= 2 && _this.game) {
                _this.gameLoaded = true;
                if(_this.game.playerX.uuid === _this.accountUUID) _this.gameLocked = false;
            }
        },

        addAnnouncement: function(message: string, type: string = "info", timeout: number = 3000): void {
            addAnnouncement(this, message, type, timeout);
        },

        updateCell: function(_cell: any, index: number): void {
            const cell = _cell as HTMLElement;
            const _this = this as any;
            if(_this.gameLocked || cell.classList.contains("x") || cell.classList.contains("o")) return;

            cell.classList.add(_this.game?.currentPlayer.toLowerCase());
            _this.gameLocked = true;



            // zjištění indexu buňky
            const x = Math.floor(index / 15);
            const y = index % 15;

            // odeslání do websocketu
            _this.socket.send(JSON.stringify({
                action: "MakeMove",
                x: x,
                y: y,
            }));

            _this.game?.currentPlayer === "X" ? _this.game.currentPlayer = "O" : _this.game.currentPlayer = "X";
        },

        generateRandomWinMessage: function(winner: string): string {
            const messages = [
                `Gratulujeme! Hráč ${winner} získal vítězství!`,
                `Výhra pro hráče ${winner}!`,
                `Hráč ${winner} zvítězil!`,
                `Gratulujeme! Hráč ${winner} je vítěz!`,
                `Hráč ${winner} triumfoval! Skvělá hra!`,
                `Hráč ${winner} ukázal, kdo je tady nejlepší!`,
                `Hráč ${winner} pokořil soupeře! Bravo!`,
                `Neskutečný výkon! Hráč ${winner} je vítěz!`,
                `Hráč ${winner} si zaslouženě připisuje vítězství!`,
                `Hráč ${winner} dominuje! Výhra je jeho!`,
                `${winner} bere korunu vítězství! Gratulace!`
            ]

            return messages[Math.floor(Math.random() * messages.length)];
        },

        initializeGame: function(data: any): void {
            const _this = this as any;
            if(data === null) throw new Error("Data is null");


            _this.game = data;
            _this.gameLocked = true;
            //console.log(data);
            //window.scroll({top: 0, left: 0, behavior: "smooth"});


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


                //if(data.winner.toLowerCase() === "x") this.addAnnouncement(_this.generateRandomWinMessage("X"), "pskr_xwin", 5000);
                //if(data.winner.toLowerCase() === "o") this.addAnnouncement(_this.generateRandomWinMessage("O"), "pskr_owin", 5000);
            }


            // zjištění, kdo je na tahu
            if(_this.accountUUID === _this.game.playerX.uuid && _this.game.currentPlayer === "X") _this.gameLocked = false;
            if(_this.accountUUID === _this.game.playerO.uuid && _this.game.currentPlayer === "O") _this.gameLocked = false;

            // v pripade ze je hra ukoncena, zobrazi se endgame veci
            if(data.winner !== null) {
                setTimeout(() => {
                    _this.showEndGameScreen();
                }, 3000);
            }
        },

        setPlayerColor: function (): any {
            const _this = this as any;
            if(!_this.game) return;

            if(_this.game?.winner?.toUpperCase() === "X") return "var(--accent-color-secondary)";
            if(_this.game?.winner?.toUpperCase() === "O") return "var(--accent-color-primary)";

            return _this.game?.currentPlayer?.toLowerCase() == 'x' ? 'var(--accent-color-secondary)' : 'var(--accent-color-primary)'
        },

        setPlayerColorHover: function (): any {
            const _this = this as any;
            if(_this.game?.winner?.toUpperCase() === "X") return "var(--accent-color-primary)";
            if(_this.game?.winner?.toUpperCase() === "O") return "var(--accent-color-secondary)";

            return _this.currentPlayer == 'x' ? 'var(--accent-color-primary)' : 'var(--accent-color-secondary)'
        },

        sendMessageToMultiplayerChat: function (message: string, event: Event): void {
            const _this = this as any;
            const element = event.target as HTMLInputElement;

            message = message.trim();
            if(message === "") return;


            _this.chatMessageInput = "";

            _this.socket.send(JSON.stringify({
                action: "SendChatMessage",
                message: message,
                sender: _this.accountName,
            }));
        },

        showEndGameScreen: function (): void {
            const _this = this as any;

            const bgDiv = document.querySelector(".background-f55288d9-4dcf-456d-87c4-26be60c16cdb") as HTMLElement;
            const newGameHeaderButton = document.querySelector(".header-5F015D44-0984-4A50-B52B-5319AE57C19C > .flex > .Login .newgame") as HTMLElement;
            const blurBgDiv = document.querySelector(".bg-29aa2e9f-d314-4366-a4cd-95ba0bbd1433") as HTMLElement;
            bgDiv?.classList.add("fade-out");
            _this.gameFadeOut = true;
            blurBgDiv?.classList.add("disableanimations");
            if(newGameHeaderButton) newGameHeaderButton.style.pointerEvents = "none";

            // zobrazeni end game veci
            _this.gameFazeIsEnding = true;
        },
    },

    computed: {

    },
})