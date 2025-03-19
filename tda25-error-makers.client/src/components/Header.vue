<script setup lang="ts">
    import { onMounted, onUnmounted, inject, type Ref } from 'vue';
    import { RouterLink } from 'vue-router';
    import { toggleTheme } from '@/main';

    function ouasihfdusifhi() {
        const header = document.querySelector("header");
        if(!header) return;

        if(window.scrollY > 0) {
            header.classList.add("scrolled")
        } else {
            header.classList.remove("scrolled")
        }
    }

    const isTransitioning = inject("isTransitioning") as Ref<boolean>;

    onMounted(() => {
        document.addEventListener("scroll", ouasihfdusifhi);
        document.onload = ouasihfdusifhi;
    });

    onUnmounted(() => {
        document.removeEventListener("scroll", ouasihfdusifhi);
    });
</script>





<template>
    <header class="header-5F015D44-0984-4A50-B52B-5319AE57C19C">

        <div class="flex">
            <RouterLink to="/" custom v-slot="{ navigate, href, isActive, isExactActive }">
                <div :class="['Header-Logo', { active: isActive }]" @click="navigate"></div>
            </RouterLink>

            <div :class="['Header-Menu', { locked: isTransitioning }]">
                <RouterLink to="/">Domů</RouterLink>
                <RouterLink to="/leaderboard">Žebříčky</RouterLink>
                <RouterLink to="/account">Účet</RouterLink>
                <RouterLink to="/play">Hrát</RouterLink>
            </div>

            <div class="Login">
                <div class="changetheme" v-on:click="toggleTheme()"></div>
            </div>
        </div>

        <!-- Menu pro mobily a tablety -->
        <div class="smallDevice"></div>
        <div class="Header-Logo-Small" onclick="location.href='/'"></div>
    </header>
</template>

<style scoped src="./Header.scss">

</style>
