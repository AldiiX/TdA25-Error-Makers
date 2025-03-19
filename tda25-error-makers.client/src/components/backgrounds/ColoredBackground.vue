<script setup lang="ts">
    import { ref, onMounted, onUnmounted } from 'vue';

    const props = defineProps({
        positionAbsolute: {
            type: Boolean,
            default: false
        },

        changeHeaderStyle: {
            type: Boolean,
            default: true
        }
    });

    onMounted(() => {
        if(!props.changeHeaderStyle) return;

        const body = document.querySelector("body");
        if(!body) return;

        body.classList.add("header-top-light");
    });

    onUnmounted(() => {
        if(!props.changeHeaderStyle) return;

        const body = document.querySelector("body");
        if(!body) return;

        body.classList.remove("header-top-light");
    });
</script>

<template>
    <div class="background-f55288d9-4dcf-456d-87c4-26be60c16cdb" v-bind:style="{ position: `${positionAbsolute ? 'absolute' : 'fixed'}`}"></div>
</template>

<style scoped>
    .background-f55288d9-4dcf-456d-87c4-26be60c16cdb {
        position: fixed;
        top: 50%;
        left: 50%;
        transform: translate(-50%, -50%);
        width: 110vw;
        height: 110vh;
        overflow: hidden;
        background: url("/images/bg1.png"), linear-gradient(90deg, rgba(0,112,187,1) 0%, rgba(57,90,154,1) 25%, rgba(114,68,121,1) 50%, rgba(227,24,55,1) 100%);
        background-position: center;
        background-size: cover;
        z-index: -1;
        transition-duration: 0.3s;
        transition-property: background;
        filter: blur(1vw);

        &::after {
            content: '';
            position: absolute;
            top: 0;
            left: 0;
            width: 100%;
            height: 100%;
            background: linear-gradient(0deg, black, transparent);
            transition-duration: 0.3s;
            opacity: 0.7;
        }
    }
</style>
