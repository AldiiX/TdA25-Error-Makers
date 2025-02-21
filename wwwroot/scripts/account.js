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
    },
    methods: {
        main: function () {
            const _this = this;
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
    },
    computed: {},
});
