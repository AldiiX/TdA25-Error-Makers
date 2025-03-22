<script setup lang="ts">

</script>

<template>
    <div class="bgg">
        <div class="one"></div>
        <div class="two"></div>
    </div>
</template>

<style scoped>
.bgg {
    position: fixed;
    display: flex;
    justify-content: center;
    align-items: center;
    width: 100vw;
    height: 100vh;
    filter: blur(100px);
    opacity: 0.6;
    pointer-events: none;
    z-index: -1;
    transition-duration: 0.3s;
    transition-property: background-color;

    &:is(.disableanimations) {
        .one, .two {
            animation: none !important;
        }
    }


    .one, .two {
        width: 200px;
    }

    .one {
        position: absolute;
        aspect-ratio: 1/1;
        background-color: var(--accent-color-primary);
        bottom: -50px;
        left: -50px;
        border-radius: 100%;
        animation: blobanim 10s infinite ease;
    }

    .two {
        position: absolute;
        aspect-ratio: 1/1;
        background-color: var(--accent-color-primary);
        top: -50px;
        right: -100px;
        border-radius: 100%;
        animation: blobanim 15s infinite ease;
        animation-delay: 2.5s;
    }

    @keyframes blobanim {
        0%,100% {
            transform: scale(1);
        }
        50% {
            transform: scale(1.18);
        }
    }
}
</style>
