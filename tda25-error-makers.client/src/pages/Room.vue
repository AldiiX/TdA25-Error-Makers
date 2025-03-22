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
            <p>Prezentující</p>
            <div class="User">
                <h1 class="title">Test</h1>
            </div>
            <p>Účastníci</p>

            <div class="users">
                <div class="User">
                    <h1 class="title">Test</h1>
                </div>
                <div class="User">
                    <h1 class="title">Test</h1>
                </div>
                <div class="User">
                    <h1 class="title">Test</h1>
                </div>
            </div>
        </div>
        <div class="buttons-red">
            <div class="button">
                <div class="button-leave">
                </div>
            </div>
            <div class="button">
                <div class="button-leave">
                </div>
            </div>
        </div>
    </div>
</template>

<style scoped src="./Room.scss">

</style>
