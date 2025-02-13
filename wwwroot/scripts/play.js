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
        locationHref(url) {
            window.location.href = url;
        },
    },
    computed: {},
});
