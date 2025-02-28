import { addAnnouncement, openModal } from "/scripts/functions.js";
export const vue = new Vue({
    el: "#app",
    mounted: function () {
        const _this = this;
        this.main();
        window.addEventListener("scroll", () => {
            _this.pageIsScrolled = window.scrollY > 0;
        });
    },
    data: {
        currentPlayer: null,
        modalOpened: null,
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
        accountChar: null,
        announcements: [],
        socket: null,
        chatMessages: [],
        chatMessageInput: "",
        gameFazeIsEnding: false,
        finishGameObject: null,
        myTimeLeft: null,
        playerOTimeLeft: null,
        playerXTimeLeft: null,
        gameTime: 0,
        drawRequestSent: false,
        drawRequestReceived: false,
        rematchRequestSent: false,
        rematchRequestReceived: false,
    },
    methods: {
        main: function () {
            const _this = this;
            this.connectToSocket();
        },
        openModal: function (modalId) {
            const _this = this;
            setTimeout(() => {
            }, 300);
            openModal(this, modalId);
            setTimeout(() => {
                if (modalId === "editgame") {
                }
            }, 300);
        },
        connectToSocket: function () {
            const _this = this;
            const uuid = window.location.pathname.split("/")[2];
            const locationIsLocalhost = window.location.host.includes("localhost");
            const socket = new WebSocket(`${locationIsLocalhost ? "ws" : "wss"}://${window.location.host}/ws/multiplayer/game/${uuid}`);
            _this.socket = socket;
            socket.onopen = () => {
            };
            socket.onmessage = (event) => {
                _this.receiveSocketMessage(event);
            };
        },
        receiveSocketMessage: function (event) {
            const _this = this;
            const data = JSON.parse(event.data);
            if (data.c === "UNA1")
                location.href = "/error?code=404&message=Hra skončila&buttonLink=/play";
            switch (data.action) {
                case "status":
                    {
                        _this.gameNumberOfPlayers = data.playerCount;
                        _this.myTimeLeft = data.myTimeLeft;
                        _this.playerXTimeLeft = data.playerXTimeLeft;
                        _this.playerOTimeLeft = data.playerOTimeLeft;
                        _this.accountChar = data.yourChar;
                        _this.currentPlayer = data.currentPlayer;
                        if (_this.game) {
                            if (data?.gameTime)
                                _this.game.gameTime = data.gameTime;
                            if (data?.gameTime)
                                _this.gameTime = data.gameTime;
                            _this.game.winner = data.winner;
                            _this.game.result = data.result;
                        }
                        if (data.winner !== null) {
                            setTimeout(() => {
                                _this.showEndGameScreen();
                            }, 3000);
                        }
                    }
                    break;
                case "updateGame":
                    {
                        this.initializeGame(data.game);
                    }
                    break;
                case "finishGame":
                    {
                        _this.finishGameObject = data;
                        _this.gameLocked = true;
                        _this.openModal(null);
                        if (data.winner !== null) {
                            setTimeout(() => {
                                _this.showEndGameScreen();
                            }, 3000);
                        }
                    }
                    break;
                case "chatMessage":
                    {
                        _this.chatMessages.push({
                            sender: data.sender,
                            message: data.message,
                            letter: String(data.letter).toUpperCase(),
                            isMe: data.sender === _this.accountName,
                        });
                        setTimeout(() => {
                            const chatDiv = document.querySelector(".chat-messages");
                            chatDiv.scroll({ top: chatDiv.scrollHeight + 100000, behavior: 'smooth' });
                        }, 10);
                    }
                    break;
                case "drawRequest":
                    {
                        if (!_this.drawRequestSent)
                            _this.addAnnouncement(`${data.sender} žádá o remízu. V chatu ji můžeš přijmout.`, "info", 6000);
                        else
                            _this.addAnnouncement("Remíza byla přijata. Hra končí.", "info", 3000);
                        if (!_this.drawRequestSent)
                            _this.drawRequestReceived = true;
                    }
                    break;
                case "rematchRequest":
                    {
                        if (!_this.rematchRequestSent)
                            _this.addAnnouncement(`${data.sender} chce hrát proti tobě ještě jednou. V chatu to můžeš přijmout.`, "info", 6000);
                        else
                            _this.addAnnouncement("Rematch byl přijat druhým hráčem. Začíná nová hra.", "info", 3000);
                        if (!_this.rematchRequestSent)
                            _this.rematchRequestReceived = true;
                    }
                    break;
                case "sendToMatch":
                    {
                        location.href = `/multiplayer/${data.matchUUID}`;
                    }
                    break;
            }
            if (!_this.gameLoaded && data.action === "status" && data.playerCount >= 2 && _this.game) {
                _this.gameLoaded = true;
                if (data.currentPlayer === data.yourChar)
                    _this.gameLocked = false;
            }
        },
        addAnnouncement: function (message, type = "info", timeout = 3000) {
            addAnnouncement(this, message, type, timeout);
        },
        updateCell: function (_cell, index) {
            const cell = _cell;
            const _this = this;
            if (_this.gameLocked || cell.classList.contains("x") || cell.classList.contains("o"))
                return;
            cell.classList.add(_this.game?.currentPlayer.toLowerCase());
            _this.gameLocked = true;
            const x = Math.floor(index / 15);
            const y = index % 15;
            _this.socket.send(JSON.stringify({
                action: "MakeMove",
                x: x,
                y: y,
            }));
            _this.game?.currentPlayer === "X" ? _this.game.currentPlayer = "O" : _this.game.currentPlayer = "X";
        },
        generateRandomWinMessage: function (winner) {
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
            ];
            return messages[Math.floor(Math.random() * messages.length)];
        },
        initializeGame: function (data) {
            const _this = this;
            if (data === null)
                throw new Error("Data is null");
            _this.game = data;
            _this.gameLocked = true;
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
            if (_this.accountUUID === _this.game.playerX.uuid && _this.game.currentPlayer === "X")
                _this.gameLocked = false;
            if (_this.accountUUID === _this.game.playerO.uuid && _this.game.currentPlayer === "O")
                _this.gameLocked = false;
        },
        setPlayerColor: function () {
            const _this = this;
            if (!_this.game)
                return;
            if (_this.game?.winner?.toUpperCase() === "X")
                return "var(--accent-color-secondary)";
            if (_this.game?.winner?.toUpperCase() === "O")
                return "var(--accent-color-primary)";
            return _this.game?.currentPlayer?.toLowerCase() == 'x' ? 'var(--accent-color-secondary)' : 'var(--accent-color-primary)';
        },
        setPlayerColorHover: function () {
            const _this = this;
            if (_this.game?.winner?.toUpperCase() === "X")
                return "var(--accent-color-primary)";
            if (_this.game?.winner?.toUpperCase() === "O")
                return "var(--accent-color-secondary)";
            return _this.currentPlayer == 'x' ? 'var(--accent-color-primary)' : 'var(--accent-color-secondary)';
        },
        sendMessageToMultiplayerChat: function (message, event) {
            const _this = this;
            const element = event.target;
            message = message.trim();
            if (message === "")
                return;
            _this.chatMessageInput = "";
            _this.socket.send(JSON.stringify({
                action: "SendChatMessage",
                message: message,
                sender: _this.accountName,
            }));
        },
        showEndGameScreen: function () {
            const _this = this;
            const bgDiv = document.querySelector(".background-f55288d9-4dcf-456d-87c4-26be60c16cdb");
            const newGameHeaderButton = document.querySelector(".header-5F015D44-0984-4A50-B52B-5319AE57C19C > .flex > .Login .newgame");
            const blurBgDiv = document.querySelector(".bg-29aa2e9f-d314-4366-a4cd-95ba0bbd1433");
            bgDiv?.classList.add("fade-out");
            _this.gameFadeOut = true;
            blurBgDiv?.classList.add("disableanimations");
            if (newGameHeaderButton)
                newGameHeaderButton.style.pointerEvents = "none";
            _this.gameFazeIsEnding = true;
            _this.gameLoaded = true;
        },
        parseTimeToWord: function (seconds) {
            const minutes = Math.floor(seconds / 60);
            const remainingSeconds = seconds % 60;
            return `${minutes} min${remainingSeconds <= 0 ? "" : ' ' + remainingSeconds + ' s'}`;
        },
        parseTimeToDigital: function (seconds) {
            const minutes = Math.floor(seconds / 60);
            const remainingSeconds = seconds % 60;
            return `${minutes}:${remainingSeconds < 10 ? "0" : ""}${remainingSeconds}`;
        },
        surrenderGame: function () {
            const _this = this;
            _this.socket.send(JSON.stringify({
                action: "surrender",
            }));
            _this.gameLocked = true;
            _this.openModal(null);
            _this.addAnnouncement("Hra byla ukončena - vzdal/a ses.", "info", 3000);
        },
        requestDraw: function () {
            const _this = this;
            if (_this.drawRequestSent) {
                _this.addAnnouncement("Žádost o remízu již byla odeslána.", "error", 3000);
                return;
            }
            _this.socket.send(JSON.stringify({
                action: "requestDraw",
            }));
            _this.openModal(null);
            _this.drawRequestSent = true;
            if (_this.drawRequestReceived)
                _this.addAnnouncement("Remíza byla přijata. Hra končí...", "info", 3000);
            else
                _this.addAnnouncement("Žádost o remízu odeslána druhému hráči.", "info", 3000);
        },
        requestRematch: function () {
            const _this = this;
            if (_this.rematchRequestSent) {
                _this.addAnnouncement("Žádost o rematch již byla odeslána.", "error", 3000);
                return;
            }
            _this.socket.send(JSON.stringify({
                action: "requestRematch",
            }));
            _this.openModal(null);
            _this.rematchRequestSent = true;
            if (_this.rematchRequestReceived)
                _this.addAnnouncement("Rematch byl přijat, začíná nová hra...", "info", 3000);
            else
                _this.addAnnouncement("Žádost o rematch odeslána druhému hráči.", "info", 3000);
        },
    },
    computed: {},
});
