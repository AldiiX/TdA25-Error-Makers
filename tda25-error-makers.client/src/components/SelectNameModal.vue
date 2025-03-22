<script setup lang="ts">
import { ref, defineProps } from "vue";

const props = defineProps<{ connectToWS: (name: string) => void, show: boolean }>();
const name = ref<string>("");
</script>

<template>
    <div class="modal-overlay" v-if="show">
        <div class="modal-body">
            <label for="name">Napište své jméno</label>
            <input type="text" name="name" placeholder="Zadejte jméno" v-model="name" />
            <button class="button-primary-col-sec" v-on:click="() => props.connectToWS(name)">Potvrdit</button>
        </div>
    </div>
</template>

<style scoped src="./SelectNameModal.scss"></style>
