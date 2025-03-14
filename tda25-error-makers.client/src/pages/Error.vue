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

<style scoped>
    .main {
        width: 100%;
        min-height: calc(100vh/* + 80px*/);
        position: relative;

        >.centerdiv {
            position: absolute;
            display: flex;
            justify-content: center;
            flex-direction: column;
            align-items: center;
            top: 50%;
            left: 50%;
            transform: translate(-50%, -50%);


            >h1 {
                font-size: 32px;
                margin: 0;
                text-align: center;
                margin-bottom: 8px;
            }

            >button {
                font-size: 20px;
                margin: 0;
                margin-top: 32px;
                padding: 12px 48px;
                text-transform: uppercase;
            }

            >p {
                margin: 0;
                color: var(--color-gray);
            }


            >.code {
                display: flex;
                width: fit-content;
                align-items: center;
                font-size: 200px;
                margin: 0;
                margin-bottom: 24px;

                >p {
                    margin: 0;
                    font-size: 200px;
                    font-weight: 700;
                }

                >.icon {
                    width: 250px;
                    height: 250px;
                    mask-image: url(/images/icons/zarivka_sad_bile.svg);
                    mask-size: contain;
                    mask-repeat: no-repeat;
                    mask-position: center;
                    background-color: var(--text-color);
                    margin: 0 48px;
                    animation: float 3s ease-in-out infinite alternate;
                    @keyframes float {
                        0% {
                            transform: translateY(0);
                        }

                        50% {
                            transform: translateY(20px);
                        }

                        100% {
                            transform: translateY(0);
                        }
                    }
                }
            }
        }
    }
</style>
