<script setup lang="ts">

    // zjisteni cisla roomky podle url /room/:id
    import {useRoute} from "vue-router";
    const route = useRoute();
    const roomId = route.params.id;

    import BlurBackground from "@/components/backgrounds/BlurBackground.vue";
    import {inject, onMounted, ref, type Ref} from "vue";
    const loggedUser = inject("loggedUser") as Ref<any>;
    let socket: WebSocket | null = null;
    const room = ref<any | null>(null);

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

            switch (action) {
                case "init": {
                    room.value = payload.room;
                } break;

                case "updateRoom":{
                    room.value = payload.room;
                } break;
            }
        };
    });
</script>

<template>
    <BlurBackground />

    <div class="sections">
        <div class="mainsection" v-if="room">
            <h1>Místnost {{ room.code }}</h1>

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
        <div class="buttons">
            <div class="button">
                <div class="button-icon">
                </div>
            </div>
            <div class="button">
                <div class="button-icon2">
                </div>
            </div>
            <div class="button-red">
                <div class="button-leave">
                </div>
            </div>
        </div>
    </div>
</template>

<style scoped src="./Room.scss">

</style>
