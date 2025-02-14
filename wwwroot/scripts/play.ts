// @ts-ignore
import { scrollToElement } from "/scripts/functions.js";

// @ts-ignore
export const vue = new Vue({
    el: "#app",
    mounted: function(){
        this.main();
    },






    data: {
        menuExpanded: false,
        announcements: [],

        temp: {
            /*
            *
            *   selectSingleplayerOrMultiplayer,
            *   selectSingleplayerMode,
            *
            *   selectMultiplayerMode,
            *   multiplayerModeCreateLobby,
            *   multiplayerModeQueue,
            *
            * */
            faze: "selectSingleplayerOrMultiplayer",
            selectedMode: null, // singleplayer nebo multiplayer
            selectedMultiplayerMode: null,
            selectedSingleplayerMode: null,

            multiplayerQueueWS: null,
        }
    },





    methods: {
        main: function(): void {
            const _this = this as any;
        },

        generateRandomTip: function(): string {
            const tips = [
                "Začni uprostřed: Když hraješ jako první, zvol střední políčko. Získáš tím kontrolu nad většinou směrů a zvýšíš své šance na vytvoření dvojí hrozby.",
                "Vytvářej dvojí hrozbu (fork): Snaž se nastavit situaci, kdy máš dvě možnosti, jak vytvořit řadu se třemi stejnými symboly. Soupeř se tak nedokáže obě hrozby současně zablokovat.",
                "Blokuj soupeře: Pozorně sleduj tahy protivníka a okamžitě blokuj, když zjistíš, že se blíží k vítězné kombinaci.",
                "Hraj do rohu: Rozeber si tahy tak, aby ti rohy poskytly více cest ke vítězství. Často můžeš tím odvrátit soupeřovy plány.",
                "Plánuj několik tahů dopředu: Neomezuj se jen na aktuální tah, ale snaž se předvídat, jak se hra bude vyvíjet v příštích několika kolech."
            ]

            return tips[Math.floor(Math.random() * tips.length)];
        },

        locationHref(url: string): void {
            window.location.href = url;
        },

        connectToMultiplayerQueue: function () {
            const _this = this as any;
            const socket = new WebSocket(`ws://${window.location.host}/ws/multiplayer/queue`);

            socket.onopen = function (event) {
                console.log('Connected to WebSocket.');
            };

            socket.onmessage = function (event) {
                console.log('Message from server:', event.data);
            };

            socket.onclose = function (event) {
                console.log('WebSocket connection closed:', event);
            };

            socket.onerror = function (error) {
                console.error('WebSocket error:', error);
                console.error('WebSocket readyState:', socket.readyState);
                console.error('WebSocket URL:', socket.url);
            };

            _this.temp.multiplayerQueueWS = socket;
        }
    },

    computed: {
    },
})