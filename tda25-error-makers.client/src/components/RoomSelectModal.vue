<script setup lang="ts">

const props = defineProps<{
    show: boolean
}>();


</script>

<template>
    <div class="modal-overlay" v-if="show">
        <div class="modal-body">
            <div class="inputs">
                <label for="kod">Zadej kód místnosti</label>
                <div class="input-group">
                    <input name="kod" type="text" maxlength="6" placeholder="000000"/>
                    <button class="button-primary-col-sec">Připojit se</button>
                </div>
            </div>
            <a class="text">nebo vytvořit roomku</a>
        </div>
    </div>
</template>

<style scoped src="./RoomSelectModal.scss">

</style>
