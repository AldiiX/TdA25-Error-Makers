<script setup lang="ts">
import {defineProps, defineEmits} from "vue";

const props = defineProps<{
    show: boolean
}>();

const emit = defineEmits(['close']);
</script>

<template>
    <div class="modal-overlay" v-if="show" @click.self="emit('close')">
        <div class="modal-body">
            <div class="inputs">
                <label for="kod">Zadej kód místnosti</label>
                <div class="input-group">
                    <input name="kod" type="text" maxlength="6" placeholder="000000"/>
                  <RouterLink to="/room/id">
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
