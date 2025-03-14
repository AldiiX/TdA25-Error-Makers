<script setup lang="ts">
    import { ref, onMounted, onUnmounted } from "vue";

    const projects = ref<any[] | null>(null);

    onMounted(() => {
        fetch("/api/v1/projects").then(async response => {
            const data = await response.json();
            projects.value = data;
        })
    });

    onUnmounted(() => {
        console.log("home byl odstraněn");
    });
</script>




<template>
    <section class="top">

    </section>

    <section class="main">
        <h1>Moje projekty</h1>

        <div class="parent">
            <div class="child" v-if="projects !== null" v-for="project in projects">
                <h2>{{ project.title }}</h2>
                <p>{{ project.description }}</p>
            </div>
        </div>
    </section>
</template>


<style>
.top {
    background-color: darkgreen;
    height: 80vh
}

.main {
    background-color: #1c1c1c;
    height: 500px;
}
</style>
