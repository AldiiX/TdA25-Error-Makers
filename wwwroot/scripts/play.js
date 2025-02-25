import { addAnnouncement } from "/scripts/functions.js";
export const vue = new Vue({
    el: "#app",
    mounted: function () {
        this.main();
    },
    data: {
        menuExpanded: false,
        announcements: [],
        temp: {
            faze: "selectSingleplayerOrMultiplayer",
            selectedMode: null,
            selectedMultiplayerMode: null,
            selectedSingleplayerMode: null,
            freeplayLobbyPlayers: [],
            freeplayRoomNumber: null,
            inputLobbyCode: null,
            socket: null,
        },
        freeplayQueue: {
            players: [],
            roomNumber: null,
            account: null,
            owner: null,
        },
        rankedQueue: {
            size: null,
            timeElapsed: null,
        },
        loggedAccount: null,
    },
    methods: {
        main: function () {
            const _this = this;
            const url = new URL(window.location.href);
            const faze = url.searchParams.get("faze");
            const roomNumber = url.searchParams.get("roomNumber");
            if (faze)
                _this.changeFaze(faze);
            if (roomNumber)
                _this.connectToMultiplayerFreeplayQueue(parseInt(roomNumber));
        },
        changeFaze: function (faze, func) {
            const _this = this;
            const el = document.querySelector(".main");
            if (el)
                el.classList.add("fadeout");
            if (faze === "multiplayerModeQueue") {
                _this.connectToMultiplayerRankedQueue();
                _this.temp.selectedMultiplayerMode = 'ranked';
                _this.temp.selectedMode = 'multiplayer';
            }
            if (faze === "createFreeplayLobby") {
                _this.connectToMultiplayerFreeplayQueue();
                _this.temp.selectedMultiplayerMode = 'freeplay';
            }
            setTimeout(() => {
                _this.temp.faze = faze;
                if (func != null)
                    func();
                if (el)
                    el.classList.remove("fadeout");
                if (el)
                    el.classList.add("fadein");
                const url = new URL(window.location.href);
                url.searchParams.set("faze", faze);
                window.history.pushState({}, "", url);
                console.log(_this.temp);
            }, 150);
            setTimeout(() => {
                if (el)
                    el.classList.remove("fadein");
            }, 300);
        },
        generateRandomTip: function () {
            const tips = [
                "Začni uprostřed: Když hraješ jako první, zvol střední políčko. Získáš tím kontrolu nad většinou směrů a zvýšíš své šance na vytvoření dvojí hrozby.",
                "Vytvářej dvojí hrozbu (fork): Snaž se nastavit situaci, kdy máš dvě možnosti, jak vytvořit řadu se třemi stejnými symboly. Soupeř se tak nedokáže obě hrozby současně zablokovat.",
                "Blokuj soupeře: Pozorně sleduj tahy protivníka a okamžitě blokuj, když zjistíš, že se blíží k vítězné kombinaci.",
                "Hraj do rohu: Rozeber si tahy tak, aby ti rohy poskytly více cest ke vítězství. Často můžeš tím odvrátit soupeřovy plány.",
                "Plánuj několik tahů dopředu: Neomezuj se jen na aktuální tah, ale snaž se předvídat, jak se hra bude vyvíjet v příštích několika kolech."
            ];
            return tips[Math.floor(Math.random() * tips.length)];
        },
        addAnnouncement: function (text, type = 'info', timeout = 5000) {
            addAnnouncement(this, text, type, timeout);
        },
        locationHref(url) {
            window.location.href = url;
        },
        connectToMultiplayerRankedQueue: function () {
            const _this = this;
            const locationIsLocalhost = window.location.host.includes("localhost");
            const socket = new WebSocket(`${locationIsLocalhost ? "ws" : "wss"}://${window.location.host}/ws/multiplayer/ranked/queue`);
            socket.onopen = function (event) {
                console.log('Connected to WebSocket.');
            };
            socket.onmessage = function (event) {
                console.log('Message from server:', event.data);
                const data = JSON.parse(event.data);
                if (data.action === "sendToMatch") {
                    _this.locationHref(`/multiplayer/${data.matchUUID}`);
                }
                if (data.action === "status") {
                    _this.rankedQueue.size = data.queueSize;
                    _this.rankedQueue.timeElapsed = data.queueTime;
                }
            };
            socket.onclose = function (event) {
                console.log('WebSocket connection closed:', event);
            };
            socket.onerror = function (error) {
                console.error('WebSocket error:', error);
                console.error('WebSocket readyState:', socket.readyState);
                console.error('WebSocket URL:', socket.url);
            };
            _this.socket = socket;
        },
        connectToMultiplayerFreeplayQueue: function (code = null) {
            const _this = this;
            console.log("connectToMultiplayerFreeplayQueue", code);
            if (_this.socket) {
                _this.socket.close();
            }
            const locationIsLocalhost = window.location.host.includes("localhost");
            const socket = new WebSocket(`${locationIsLocalhost ? "ws" : "wss"}://${window.location.host}/ws/multiplayer/freeplay/queue${code ? `?roomNumber=${code}` : ""}`);
            socket.onopen = function (event) {
                console.log('Connected to WebSocket.');
            };
            socket.onmessage = function (event) {
                console.log('Message from server:', event.data);
                const data = JSON.parse(event.data);
                switch (data.action) {
                    case "joined":
                        {
                            _this.freeplayQueue.roomNumber = data.roomNumber;
                            _this.freeplayQueue.players = data.players;
                            _this.freeplayQueue.account = data.yourAccount;
                            _this.freeplayQueue.owner = data.roomOwner;
                            console.warn(_this.freeplayQueue);
                        }
                        break;
                    case "lobbyDoesntExistError":
                        {
                            _this.addAnnouncement(`Místnost s kódem ${_this.temp.inputLobbyCode} neexistuje.`, "error");
                        }
                        break;
                    case "kicked":
                        {
                            _this.socket = null;
                            _this.addAnnouncement("Byl jsi vyhozen z místnosti: " + data.message, "error");
                            _this.connectToMultiplayerFreeplayQueue();
                        }
                        break;
                    case "sendToMatch":
                        {
                            _this.locationHref(`/multiplayer/${data.matchUUID}`);
                        }
                        break;
                    case "status":
                        {
                            _this.freeplayQueue.players = data.players;
                        }
                        break;
                    case "error":
                        {
                            _this.addAnnouncement(data.message, "error");
                        }
                        break;
                }
            };
            socket.onclose = function (event) {
                console.log('WebSocket connection closed:', event);
            };
            socket.onerror = function (error) {
                console.error('WebSocket error:', error);
                console.error('WebSocket readyState:', socket.readyState);
                console.error('WebSocket URL:', socket.url);
            };
            _this.socket = socket;
        },
        startFreeplayLobby: function () {
            const _this = this;
            if (!_this.freeplayQueue?.account?.isOwnerOfRoom)
                return;
            _this.socket.send(JSON.stringify({
                action: "startFreeplayLobby"
            }));
        },
        parseTimeToDigital: function (seconds) {
            const minutes = Math.floor(seconds / 60);
            const remainingSeconds = seconds % 60;
            return `${minutes}:${remainingSeconds < 10 ? "0" : ""}${remainingSeconds}`;
        },
    },
    computed: {},
});
