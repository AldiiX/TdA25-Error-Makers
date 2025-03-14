<script setup lang="ts">
    import { onMounted, onUnmounted } from 'vue';
    import { RouterLink } from 'vue-router';

    function ouasihfdusifhi() {
        const header = document.querySelector("header");
        if(!header) return;

        if(window.scrollY > 0) {
            header.classList.add("scrolled")
        } else {
            header.classList.remove("scrolled")
        }
    }

    onMounted(() => {
        document.addEventListener("scroll", ouasihfdusifhi);
        document.onload = ouasihfdusifhi;
    });

    onUnmounted(() => {
        document.removeEventListener("scroll", ouasihfdusifhi);
    });
</script>





<template>
      <header>
            <h1>Header</h1>
            <RouterLink to="/">Home</RouterLink>
            <RouterLink to="/about">About</RouterLink>
      </header>
</template>

<style scoped>
header {
    background-color: #333;
    color: white;
    padding: 1rem;
    width: 100%;
    justify-content: space-between;
    align-items: center;
    position: absolute;

    &:is(.scrolled) {
        background-color: #006949;
        position: fixed;
    }
}
</style>
