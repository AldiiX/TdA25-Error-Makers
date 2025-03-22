<script setup lang="ts">
import {defineProps, defineEmits, ref} from "vue";

const props = defineProps<{
    show: boolean
}>();

const inputText = ref<number>(100_000);
const emit = defineEmits(['close']);
</script>

<template>
    <div class="modal-overlay" v-if="show" @click.self="emit('close')">
        <div class="modal-body">
            <div class="inputs">
                <label for="kod">Zadej kód místnosti</label>
                <div class="input-group">
                    <input name="kod" min="100000" max="999_999" maxlength="6" v-model="inputText" type="number" placeholder="000000"/>
                  <RouterLink v-bind:to="'/room/' + inputText">
                    <button class="button-primary-col-sec">Připojit se</button>
                  </RouterLink>
                </div>
            </div>
            <a class="text">nebo vytvořit roomku</a>
        </div>
    </div>
</template>

<style scoped src="./RoomSelectModal.scss">

</style>
