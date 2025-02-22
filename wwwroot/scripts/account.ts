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

                _this.gameHistory = data;
                //console.log(data);
            })
        },
        
        getGameResult: function (game: any) {
            if (game.loggeduserwon){
                return "win";
            }
            else if (!game.loggeduserwon){
                return "lose";
            }
            else {
                return "draw";
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

                //console.log(data);
            })
        }
    },

    computed: {
    },
});