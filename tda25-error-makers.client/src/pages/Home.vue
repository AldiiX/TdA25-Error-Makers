<script setup lang="ts">
    import { ref, onMounted, onUnmounted } from "vue";
    import BlurBackground from "@/components/backgrounds/BlurBackground.vue";
    import ColoredBackground from "@/components/backgrounds/ColoredBackground.vue";
    import { RouterLink } from "vue-router";
    import RoomSelectModal from "@/components/RoomSelectModal.vue";

    const shownModal = ref(false);


</script>




<template>
    <section class="top">
        <BlurBackground />
      <RoomSelectModal v-bind:show="shownModal" @close="shownModal = false" />

        <div class="center">
            <div class="left">
                <h1>Think different Academy</h1>
                <p>Aplikace plná inovací</p>

              <button class="button-primary-col-sec" @click="shownModal = true">
                Připojit se do místnosti
              </button>
            </div>

            <div class="right">
                <div class="img"></div>
            </div>
        </div>
    </section>
</template>


<style scoped src="./Home.scss">

</style>
