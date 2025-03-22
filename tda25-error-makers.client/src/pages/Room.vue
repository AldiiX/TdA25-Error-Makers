<script setup lang="ts">

    // zjisteni cisla roomky podle url /room/:id
    import {useRoute} from "vue-router";
    const route = useRoute();
    const roomId = route.params.id;

    import BlurBackground from "@/components/backgrounds/BlurBackground.vue";
    import {inject, onMounted, ref, type Ref} from "vue";
    import BlurBlueBackground from "@/components/backgrounds/BlurBlueBackground.vue";
    import SelectNameModal from "@/components/SelectNameModal.vue";
    const loggedUser = inject("loggedUser") as Ref<any>;
    let socket: WebSocket | null = null;
    const room = ref<any | null>(null);
    const nameModalShown = ref<boolean>(true);



    function connectToWS(name: string) {
        let queryParams = [];
        if(roomId) queryParams.push(`roomNumber=${roomId}`);
        queryParams.push(`username=${loggedUser?.value?.name ?? name}`);


        socket = new WebSocket(`${window.location.protocol === 'https:' ? 'wss' : 'ws'}://${window.location.host}/ws/room?${queryParams.join('&')}`);
        console.log(socket);
        nameModalShown.value = false;

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
    }

    function setUserAsPresenter(uuid: string) {
        socket?.send(JSON.stringify({
            action: "setPresenter",
            userUUID: uuid
        }));
    }
</script>

<template>
    <BlurBlueBackground />
    <SelectNameModal :connectToWS v-bind:show="nameModalShown" />

    <div class="sections">
        <div class="mainsection">
                <p>Prezentující</p>
            <div class="User" v-for="user in room.connectedUsers" v-if="room?.connectedUsers">
              <template v-if="user.isPresenter">
                <p class="title">{{ user.name[0].toUpperCase() }}</p>
                <p class="username">{{ user.name }}</p>
              </template>
            </div>
            <p>Účastníci</p>

            <div class="users">
                <div class="User" v-for="user in  room.connectedUsers" v-if="room?.connectedUsers">
                    <p class="title">{{ user.name[0].toUpperCase() }}</p>
                    <p class="username">{{ user.name }}</p>
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
