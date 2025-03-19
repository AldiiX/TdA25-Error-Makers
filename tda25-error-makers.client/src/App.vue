<script setup lang="ts">
    import { RouterView  } from 'vue-router';
    import Header from "@/components/Header.vue";
    import Footer from "@/components/Footer.vue";
    import { ref, watch, provide, onMounted, inject, type Ref } from "vue";
    import { useRoute } from "vue-router";
    import { getTheme } from "./main.ts";

    const isTransitioning = inject("isTransitioning") as Ref<boolean>;

    onMounted(() => {
        getTheme();
    });
</script>







<template>
    <div id="transition-div" v-if="isTransitioning"></div>

    <Header />

    <RouterView />

    <Footer />
</template>







<style scoped>
    #transition-div {
        position: fixed;
        top: 0;
        left: 0;
        width: 100%;
        height: 100vh;
        background-color: var(--background-color-3);
        z-index: 4;
        animation: aiodjdsoifidosjf 0.15s ease-out;
        animation-iteration-count: 1;

        &:is(.fade-in) {
            animation: aiodjdsoifidosjf 0.15s;
            animation-iteration-count: 1;
        }

        &:is(.fade-out) {
            animation-iteration-count: 1;
            animation: adsaads 0.15s;
        }
    }

    @keyframes aiodjdsoifidosjf {
        0% {
            opacity: 0;
        }

        100% {
            opacity: 1;
        }
    }

    @keyframes adsaads {
        0% {
            opacity: 1;
        }

        100% {
            opacity: 0;
        }
    }
</style>
