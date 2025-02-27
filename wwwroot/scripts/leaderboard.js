export const vue = new Vue({
    el: "#app",
    mounted: function () {
        this.main();
    },
    data: {
        menuExpanded: false,
        announcements: [],
        leaderboard: [],
    },
    methods: {
        main: function () {
            const _this = this;
            _this.getLeaderboard();
        },
        getLeaderboard: function () {
            const _this = this;
            fetch("/api/v2/leaderboard", {
                method: "GET",
            }).then(async (response) => {
                const data = await response.json();
                if (!response.ok) {
                    console.error("požadavek nebyl uspesny");
                    return;
                }
                _this.leaderboard = data;
            });
        },
        getInitials: function (user) {
            if (!user)
                return "";
            const name = user.display_name || user.username;
            return name ? name.charAt(0).toUpperCase() : "";
        }
    },
    computed: {
        topThree: function () {
            const _this = this;
            return _this.leaderboard.slice(0, 3);
        },
        restOfLeaderboard: function () {
            const _this = this;
            return _this.leaderboard.slice(3);
        },
    },
});
