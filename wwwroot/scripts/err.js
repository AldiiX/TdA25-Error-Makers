export const vue = new Vue({
    el: "#app",
    mounted: function () {
        this.main();
    },
    data: {
        menuExpanded: false,
        announcements: [],
    },
    methods: {
        main: function () {
            const _this = this;
        },
    },
    computed: {},
});
