// @ts-ignore
import { scrollToElement, addAnnouncement } from "/scripts/functions.js";

// @ts-ignore
export const vue = new Vue({
    el: "#app",
    mounted: function(){
        this.main();
    },
    
        data: {
            menuExpanded: false,
            announcements: [],
            currentTab: "GameHistory",
            username: "",
        },

        methods: {
            main: function (): void {
                const _this = this as any;
            },
            
            changeCredentialsFormSubmit: function(event: Event) {
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
                    
                    if(!response.ok) {
                        console.error("požadavek nebyl uspesny");
                        return;
                    }
                    
                    window.location.reload();
                })
            },
            
            deleteaccount: function() {
                const _this = this as any;
                fetch("/api/v2/myaccount", {
                    method: "DELETE",
                }).then(async response => {
                    const data = await response.json();
                    
                    if(!response.ok) {
                        console.error("požadavek nebyl uspesny");
                        return;
                    }
                    
                    window.location.href="/";
                })
            },
            
        },
    computed: {
    },
});