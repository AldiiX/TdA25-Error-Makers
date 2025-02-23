export const vue = new Vue({
    el: "#app",
    mounted: function () {
        this.main();
    },
    data: {
        menuExpanded: false,
        announcements: [],
        currentTab: "GameHistory",
        username: "",
        gameHistory: [],
        users: [],
        usersFiltered: [],
        searchUserInput: "",
    },
    methods: {
        main: function () {
            const _this = this;
            _this.getGameHistory();
            _this.getUsers();
        },
        changeCredentialsFormSubmit: function (event) {
            const _this = this;
            event.preventDefault();
            const form = event.target;
            const formData = new FormData(form);
            const data = {
                username: formData.get("username"),
                displayName: formData.get("displayName"),
                oldPassword: formData.get("oldPassword"),
                email: formData.get("email"),
                newPassword: formData.get("newPassword"),
            };
            fetch("/api/v2/credentials", {
                method: "PUT",
                headers: {
                    "Content-Type": "application/json",
                },
                body: JSON.stringify(data)
            }).then(async (response) => {
                const data = await response.json();
                if (!response.ok) {
                    console.error("požadavek nebyl uspesny");
                    return;
                }
                window.location.reload();
            });
        },
        deleteaccount: function () {
            const _this = this;
            fetch("/api/v2/myaccount", {
                method: "DELETE",
            }).then(async (response) => {
                const data = await response.json();
                if (!response.ok) {
                    console.error("požadavek nebyl uspesny");
                    return;
                }
                window.location.href = "/";
            });
        },
        getGameHistory: function () {
            const _this = this;
            fetch("/api/v2/gamehistory", {
                method: "GET",
            }).then(async (response) => {
                const data = await response.json();
                if (!response.ok) {
                    console.error("požadavek nebyl uspesny");
                    return;
                }
                _this.gameHistory = data;
            });
        },
        getGameResult: function (game) {
            if (game.loggeduserwon) {
                return "win";
            }
            else if (!game.loggeduserwon) {
                return "lose";
            }
            else {
                return "draw";
            }
        },
        getUsers: function () {
            const _this = this;
            fetch("/api/v2/users", {
                method: "GET",
            }).then(async (response) => {
                const data = await response.json();
                if (!response.ok) {
                    console.error("požadavek nebyl uspesny");
                    return;
                }
                _this.users = data;
                _this.usersFiltered = data;
            });
        },
        banUser: function (user) {
            const _this = this;
            fetch(`/api/v2/users/${user.uuid}/ban`, {
                method: "PUT",
                headers: {
                    "Content-Type": "application/json",
                },
            }).then(async (response) => {
                const data = await response.json();
                if (!response.ok) {
                    console.error("požadavek nebyl uspesny");
                    return;
                }
                _this.getUsers();
            });
        },
        unbanUser: function (user) {
            const _this = this;
            fetch(`/api/v2/users/${user.uuid}/unban`, {
                method: "PUT",
                headers: {
                    "Content-Type": "application/json",
                },
            }).then(async (response) => {
                const data = await response.json();
                if (!response.ok) {
                    console.error("požadavek nebyl uspesny");
                    return;
                }
                _this.getUsers();
            });
        },
        filterUsers: function () {
            const _this = this;
            _this.usersFiltered = _this.users.filter((user) => {
                return user.username.toLowerCase().includes(_this.searchUserInput.toLowerCase()) || user.display_name?.toLowerCase().includes(_this.searchUserInput.toLowerCase());
            });
        },
    },
    computed: {},
});
