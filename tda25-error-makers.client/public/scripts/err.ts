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
    },





    methods: {
        main: function(): void {
            const _this = this as any;
        },
    },

    computed: {
    },
})