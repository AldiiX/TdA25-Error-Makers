<script setup lang="ts">

    // zjisteni cisla roomky podle url /room/:id
    import {useRoute} from "vue-router";
    const route = useRoute();
    const roomId = route.params.id;

    import BlurBackground from "@/components/backgrounds/BlurBackground.vue";
    import {inject, onMounted, type Ref} from "vue";
    const loggedUser = inject("loggedUser") as Ref<any>;
    let socket: WebSocket | null = null;

    onMounted(() => {
        socket = new WebSocket(`${window.location.protocol === 'https:' ? 'wss' : 'ws'}://${window.location.host}/ws/room${roomId ? "?roomNumber=" + roomId : ""}`);
        console.log(socket);

        socket.onopen = () => {
            console.log('Connected to the server');
        };

        socket.onmessage = (event) => {
            const payload = JSON.parse(event.data);
            console.log(payload);
            const action = payload.action;
        };
    });
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
