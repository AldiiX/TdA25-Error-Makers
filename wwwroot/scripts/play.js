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
            account: null
        }
    },
    methods: {
        main: function () {
            const _this = this;
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
    },
    computed: {},
});
