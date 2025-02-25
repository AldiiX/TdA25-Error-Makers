// @ts-ignore
import { scrollToElement, addAnnouncement } from "/scripts/functions.js";

// @ts-ignore
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
        showBoard: false,
    },

    methods: {
        main: function (): void {
            const _this = this as any;
            _this.getGameHistory();
            _this.getUsers();
        },

        changeCredentialsFormSubmit: function (event: Event) {
            const _this = this as any;
            event.preventDefault();
            const form = event.target as HTMLFormElement;
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
            }).then(async response => {
                const data = await response.json();

                if (!response.ok) {
                    console.error("požadavek nebyl uspesny");
                    return;
                }

                window.location.reload();
            })
        },

        deleteaccount: function () {
            const _this = this as any;
            fetch("/api/v2/myaccount", {
                method: "DELETE",
            }).then(async response => {
                const data = await response.json();

                if (!response.ok) {
                    console.error("požadavek nebyl uspesny");
                    return;
                }

                window.location.href = "/";
            })
        },
        
        getGameHistory: function () {
            const _this = this as any;
            fetch("/api/v2/gamehistory", {
                method: "GET",
            }).then(async response => {
                const data = await response.json();

                if (!response.ok) {
                    console.error("požadavek nebyl uspesny");
                    return;
                }

                //console.log(data);
                _this.gameHistory = data;
            })
        },
        
        getGameResult: function (game: any) {
            if (game.loggeduserwon){
                return "Výhra";
            }
            else if (!game.loggeduserwon){
                return "Prohra";
            }
            else {
                return "Remíza";
            }
        },
        
        getUsers: function () {
            const _this = this as any;
            fetch("/api/v2/users", {
                method: "GET",
            }).then(async response => {
                const data = await response.json();

                if (!response.ok) {
                    console.error("požadavek nebyl uspesny");
                    return;
                }
                
                _this.users = data;
                _this.usersFiltered = data;

                //console.log(data);
            })
        },
        
        banUser: function (user: any) {
            const _this = this as any;
            fetch(`/api/v2/users/${user.uuid}/ban`, {
                method: "PUT",
                headers: {
                    "Content-Type": "application/json",
                },
            }).then(async response => {
                const data = await response.json();

                if (!response.ok) {
                    console.error("požadavek nebyl uspesny");
                    return;
                }

                _this.getUsers();
            })
        },
        
        unbanUser: function (user: any) {
            const _this = this as any;
            fetch(`/api/v2/users/${user.uuid}/unban`, {
                method: "PUT",
                headers: {
                    "Content-Type": "application/json",
                },
            }).then(async response => {
                const data = await response.json();

                if (!response.ok) {
                    console.error("požadavek nebyl uspesny");
                    return;
                }

                _this.getUsers();
            })
        },
        
        filterUsers: function () {
            const _this = this as any;
            _this.usersFiltered = _this.users.filter((user: any) => {
                return user.username.toLowerCase().includes(_this.searchUserInput.toLowerCase()) || user.display_name?.toLowerCase().includes(_this.searchUserInput.toLowerCase());
            });
        },

        isWinningCell: function (index: number, game :any): any {
            //console.log(game);
            const boardSize = 15; // velikost hrací desky 15x15
            const row = Math.floor(index / boardSize);
            const col = index % boardSize;
            return game.winningCells?.some((cell: any) => cell[0] === row && cell[1] === col);
        },

    },

    computed: {
    },
});