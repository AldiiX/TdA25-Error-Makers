<script setup lang="ts">
    import BlurBackground from "@/components/backgrounds/BlurBackground.vue";

    const props = defineProps<{
        code?: number;
        message?: string;
        backWord?: string;
    }>();

    const code = String(props.code);
    const message = props.message ?? "Něco se pokazilo";
    const backWord = props.backWord ?? "Zpět";
</script>

<template>
    <BlurBackground />

    <div class="main">
        <div class="centerdiv">
            <div class="code">
                <p>{{ code[0] }}</p>
                <div class="icon" v-if="code[1] === '0'"></div>
                <p v-else>{{ code[1] }}</p>
                <p>{{ code[2] }}</p>
            </div>
            <h1>{{ message }}</h1>
            <p>XOOXO</p>
            <button class="button-primary-col-sec">{{ backWord }}</button>
        </div>
    </div>
</template>

<style scoped src="./Error.scss">

</style>
