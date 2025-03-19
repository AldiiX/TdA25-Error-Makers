<script setup lang="ts">
    import { useSlots } from "vue";
    import ColoredBackground from "@/components/backgrounds/ColoredBackground.vue";

    const props = defineProps({
        text: {
            type: String,
            default: `zadej text jako text="nadpis"`
        }
    });

    const slots = useSlots();
</script>

<template>
    <div class="sections">
        <section class="top">
            <ColoredBackground :positionAbsolute="true" />

            <!-- pokud existuje top slot, tak se pak odstrani default -->
            <slot name="top" />

            <div v-if="!slots.top" class="texts">
                <h1>{{ props.text }}</h1>
            </div>
        </section>
        <section class="mainsection">
            <slot />
        </section>
    </div>
</template>

<style scoped src="./ColoredBGLayout.scss">
</style>
