<script setup lang="ts">
    interface ChatMessage {
        id: string;
        content: string;
        sender: string;
        timestamp: string;
    }

    import {onMounted, ref} from 'vue';
    const chatMessages = ref<ChatMessage[]>([]);
    let socket: WebSocket | null = null;
    let inputMessage = '';

    onMounted(() => {
        socket = new WebSocket(`${window.location.protocol === 'https:' ? 'wss' : 'ws'}://${window.location.host}/ws/chat`);
        console.log(socket);

        socket.onopen = () => {
            console.log('Connected to the server');
        };

        socket.onmessage = (event) => {
            const payload = JSON.parse(event.data);
            //console.log(payload);
            const action = payload.action;

            if (action === 'message') {
                chatMessages.value.push(payload.message);
                console.log(chatMessages.value);
            }

            if (action === 'init') {
                chatMessages.value = payload.messages;
            }
        };
    });


    function sendMessage() {
        if(socket === null) return;

        //console.log(inputMessage)

        socket.send(JSON.stringify({
            action: 'chatMessage',
            message: inputMessage
        }));
    }
</script>

<template>
    <div class="chatbox" style="margin-bottom: 1000px">
        <div class="chatbox-top">
            <div class="chat-content" v-for="chatMessage in chatMessages">
                <div class="message">
                    <div>{{ chatMessage.content }}</div>
                    <div>{{ chatMessage.sender }}</div>
                </div>
            </div>

            <input type="text" v-model="inputMessage">
            <button v-on:click="sendMessage()">Send</button>
        </div>
    </div>
</template>

<style scoped>

</style>
