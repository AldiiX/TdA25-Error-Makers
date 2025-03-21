<script setup lang="ts">
    interface ChatMessage {
        id: string;
        message: string;
        sender: string;
        timestamp: string;
    }

    import {onMounted, ref} from 'vue';
    const chatMessages = ref<ChatMessage[]>([]);
    let socket: WebSocket | null = null;

    onMounted(() => {
        socket = new WebSocket(`${window.location.protocol === 'https:' ? 'wss' : 'ws'}://${window.location.host}/ws/chat`);
        console.log(socket);

        socket.onopen = () => {
            console.log('Connected to the server');
        };

        socket.onmessage = (event) => {
            const payload = JSON.parse(event.data);
            console.log(payload);
            const action = payload.action;

            if (action.type === 'chatMessage') {
                chatMessages.value.push(payload.message);
            }

            if (action.type === 'init') {
                chatMessages.value = payload.messages;
            }
        };
    });
</script>

<template>
    <div class="chatbox">
        <div class="chatbox-top">

        </div>
    </div>
</template>

<style scoped>

</style>
