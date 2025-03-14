<script setup lang="ts">
    import { RouterView  } from 'vue-router';
    import Header from "@/components/Header.vue";
    import Footer from "@/components/Footer.vue";
    import { ref, watch } from "vue";
    import { useRoute } from "vue-router";
    const route = useRoute();

    watch(route, () => {
        document.title = String(route.name) + " • Think Different Academy";
    }, { immediate: true });
</script>







<template>
    <Header />

    <RouterView />

    <Footer />
</template>







<style scoped>

</style>
