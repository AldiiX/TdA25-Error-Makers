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
        leaderboard: [],
    },





    methods: {
        main: function(): void {
            const _this = this as any;
            _this.getLeaderboard();
        },

        getLeaderboard: function () {
            const _this = this as any;
            fetch("/api/v2/leaderboard", {
                method: "GET",
            }).then(async response => {
                const data = await response.json();

                if (!response.ok) {
                    //console.error("požadavek nebyl uspesny");
                    return;
                }
                _this.leaderboard = data;
                //console.log(data);
            })
        },

        getInitials: function(user: any) {
            if (!user) return "";
            const name = user.display_name || user.username;
            return name ? name.charAt(0).toUpperCase() : "";
        },

        locationHref: function (url: string) {
            location.href = url;
        },
    },





    computed: {
        topThree: function() {
            const _this = this as any;
            // Vrátí 1.–3. místo
            return _this.leaderboard.slice(0, 3);
        },

        restOfLeaderboard: function() {
            const _this = this as any;
            // Vrátí 4. místo a dále
            return _this.leaderboard.slice(3);
        },
    },
})
