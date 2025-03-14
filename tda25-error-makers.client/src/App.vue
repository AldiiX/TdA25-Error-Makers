<script setup lang="ts">
    import { RouterView  } from 'vue-router';
    import Header from "@/components/Header.vue";
    import Footer from "@/components/Footer.vue";
    import { ref, watch, provide, onMounted } from "vue";
    import { useRoute } from "vue-router";
    import { getTheme } from "./main.ts";
    const route = useRoute();

    watch(route, () => {
        document.title = String(route.name) + " • Think Different Academy";
    }, { immediate: true });

    onMounted(() => {
        getTheme();
    });
</script>







<template>
    <Header />

    <RouterView />

    <Footer />
</template>







<style scoped>

</style>
