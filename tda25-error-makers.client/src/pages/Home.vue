<script setup lang="ts">
    import { ref, onMounted, onUnmounted } from "vue";
    import BlurBackground from "@/components/backgrounds/BlurBackground.vue";
    import ColoredBackground from "@/components/backgrounds/ColoredBackground.vue";

    const projects = ref<any[] | null>(null);

    onMounted(() => {
        fetch("/api/v1/projects").then(async response => {
            const data = await response.json();
            projects.value = data;
        })
    });
</script>




<template>
    <section class="top">
        <ColoredBackground />

        <div class="center">
            <div class="left">
                <h1>Think different Academy</h1>
                <p>Platforma na řešení zajímavých piškvorkových úloh, která ti pomůže rozvíjet logické myšlení a strategické schopnosti.</p>
                <button class="button-primary-col-sec" onclick="location.href='/play'">HRÁT</button>
            </div>

            <div class="right">
                <div class="img"></div>
            </div>
        </div>
    </section>
</template>


<style scoped src="./Home.scss">

</style>
