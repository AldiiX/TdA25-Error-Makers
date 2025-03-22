<script setup lang="ts">

import BlurBackground from "@/components/backgrounds/BlurBackground.vue";
import {inject, type Ref} from "vue";
const loggedUser = inject("loggedUser") as Ref<any>;
console.log(loggedUser);

</script>

<template>
    <BlurBackground />

    <div class="sections">
        <div class="mainsection">
            <h1>Místnost</h1>
            <div class="users">
                <div class="User">
                    <h2 class="title">Test</h2>
                </div>
                <div class="User">
                    <h2 class="title">Test</h2>
                </div>
            </div>
        </div>
        <div class="buttons"></div>
    </div>

    <div v-if="loggedUser">
        přihlášen jako {{ loggedUser.name }}
    </div>
</template>

<style scoped src="./Room.scss">

</style>
