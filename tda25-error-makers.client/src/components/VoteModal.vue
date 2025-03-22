<script setup lang="ts">
import {defineProps, defineEmits, ref} from "vue";

const props = defineProps<{
    show: boolean;
}>();

const emit = defineEmits<{
    (event: 'close'): void;
}>();

const VoteName = ref<string>("");
</script>

<template>
    <div class="modal-overlay" v-if="props.show">
        <div class="modal-body">
            <label for="kod">Zadej název hlasování.</label>
            <input name="name" v-model="VoteName" type="text" placeholder="Zadejte název."/>
            <button class="button-primary-col-sec" @click="emit('close')">Začít hlasování</button>
        </div>
    </div>
</template>

<style scoped src="./VoteModal.scss">

</style>
