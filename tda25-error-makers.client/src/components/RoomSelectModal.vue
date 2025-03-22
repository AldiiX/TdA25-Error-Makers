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
                    <input name="kod" type="text" maxlength="6" placeholder="Zadejte kód místnosti"/>
                    <button class="button-primary-col-sec">Připojit se</button>
                </div>
            </div>
            <a class="text">nebo vytvořit roomku</a>
        </div>
    </div>
</template>

<style scoped>
.modal-overlay {
    position: fixed;
    backdrop-filter: blur(10px) brightness(90%);
    width: 100%;
    height: 100%;
    z-index: 1;

    >.modal-body {
        position: fixed;
        width: 320px;
        top: 50%;
        left: 50%;
        transform: translate(-50%, -50%);
        z-index: 2;
        background-color: white;
        padding: 48px;
        border-radius: 8px;

        >.inputs{
            >label{
                font-size: 24px;
                font-weight: 600;
                color: var(--text-color);
            }

            >.input-group{
                display: flex;
               justify-content: space-between;

                >input{
                    width: 45%;
                }
            }
        }
    }
}
</style>
