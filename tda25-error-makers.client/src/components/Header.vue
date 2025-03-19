<script setup lang="ts">
    import { onMounted, onUnmounted } from 'vue';
    import { RouterLink } from 'vue-router';
    import { toggleTheme } from '@/main';

    function ouasihfdusifhi() {
        const header = document.querySelector("header");
        if(!header) return;

        if(window.scrollY > 0) {
            header.classList.add("scrolled")
        } else {
            header.classList.remove("scrolled")
        }
    }

    onMounted(() => {
        document.addEventListener("scroll", ouasihfdusifhi);
        document.onload = ouasihfdusifhi;
    });

    onUnmounted(() => {
        document.removeEventListener("scroll", ouasihfdusifhi);
    });
</script>





<template>
    <header class="header-5F015D44-0984-4A50-B52B-5319AE57C19C">

        <div class="flex">
            <RouterLink to="/" custom v-slot="{ navigate, href, isActive, isExactActive }">
                <div :class="['Header-Logo', { active: isActive }]" @click="navigate"></div>
            </RouterLink>

            <div class="Header-Menu">
                <RouterLink to="/">Domů</RouterLink>
                <RouterLink to="/leaderboard">Žebříčky</RouterLink>
                <RouterLink to="/account">Účet</RouterLink>
                <RouterLink to="/play">Hrát</RouterLink>
            </div>

            <div class="Login">
                <div class="changetheme" v-on:click="toggleTheme()"></div>
            </div>
        </div>

        <!-- Menu pro mobily a tablety -->
        <div class="smallDevice"></div>
        <div class="Header-Logo-Small" onclick="location.href='/'"></div>
    </header>
</template>

<style scoped>
body:is(.header-top-light) .header-5F015D44-0984-4A50-B52B-5319AE57C19C:not(.scrolled) {
    >.flex {
        >.Header-Menu >a {
            color: white !important;

            &:is(.active)::before, &:is(.router-link-active)::before {
                background: white !important;
                transition-duration: 0.3s;
            }
        }

        >.Login >.newgame {
            background-color: rgba(255,255,255, 0.2);
        }

        >.Login >.changetheme {
            background-color: white;
        }

        >.Header-Logo {
            background-image: url("/images/svg/Think-different-Academy_LOGO_oficialni-bile.svg");
        }
    }

    > .Header-Logo-Small, .smallDevice {
        background-color: white;
    }
}

.header-5F015D44-0984-4A50-B52B-5319AE57C19C {
    position: fixed;
    background: none;
    border-radius: 40px;
    width: 90%;
    height: 80px;
    margin-top: 2.5vh;
    left: 50%;
    transform: translateX(-50%);
    top: 0;
    z-index: 5;
    transition-property: background, opacity;
    transition-duration: 0.3s;

    &:is(.scrolled) {
        background: var(--background-color-3);

        .newgame {
            background-color: var(--accent-color-primary) !important;
            transition-duration: 0.3s;

            &:hover {
                background-color: var(--accent-color-primary-darker) !important;
            }
        }
    }

    >.flex {
        position: relative;
        display: flex;
        height: 100%;
        width: 88.6%;
        align-items: center;
        justify-content: space-between;
        margin: 0 auto;

        >.Login {
            width: fit-content;
            display: flex;
            align-items: center;
            gap: 24px;

            >.User{
                display: flex;
                align-items: center;
                gap: 8px;
                margin-left: 32px;
                transition-duration: 0.3s;

                &:hover {
                    cursor: pointer;
                    opacity: 0.5;
                    transition-duration: 0.3s;
                }

                .UserIcon{
                    font-family: "Dosis", sans-serif;
                    font-size: 24px;
                    font-weight: 700;
                    color: white;
                    text-align: center;
                    align-content: center;
                    position: relative;
                    display: block;
                    border-radius: 50%;
                    width: 50px;
                    height: 50px;
                }
                .Texts{
                    >.top,
                    >.bot{
                        p{
                            text-align: right;
                            color : white;
                            margin: 1px;
                        }
                    }
                    .bot{
                        p{
                            font-weight: 700;
                        }
                    }
                }
            }

            >.changetheme {
                width: 30px;
                height: 30px;
                background-color: var(--text-color);
                transition-duration: 0.3s;
                mask-image: var(--theme-icon);
                mask-size: contain;
                mask-repeat: no-repeat;
                mask-position: center;
                cursor: pointer;

                &:hover {
                    background-color: var(--color-gray) !important;
                    transition-duration: 0.3s;
                }
            }

            >.newgame{
                border: none;
                border-radius: 12px;
                padding: 10px 20px;
                cursor: pointer;
                margin: 0;

                &:hover {
                    background: rgba(255,255,255, 0.3);
                    transition-duration: 0.3s;
                }
            }
        }

        >.Header-Logo {
            height: 50px;
            aspect-ratio: 150/47;
            display: flex;
            background-image: var(--footer-tda-logo);
            background-size: contain;
            background-repeat: no-repeat;
            background-position: center;
            cursor: pointer;
            transition-duration: 0.3s;
        }

        >.Header-Menu {
            position: absolute;
            display: flex;
            gap: 40px;
            align-items: center;
            justify-content: center;
            height: 100%;
            width: fit-content;
            top: 50%;
            left: 360px;
            transform: translate(-50%, -50%);

            a {
                position: relative;
                text-decoration: none;
                font-weight: 1000;
                color: var(--text-color-primary);
                user-select: none;
                transition-duration: 0.3s;

                &:is(.active), &:is(.router-link-active) {
                    &::before {
                        content: "";
                        display: block;
                        width: 100%;
                        height: 2px;
                        background: var(--accent-color-primary);
                        margin-top: 22.5px;
                        position: absolute;
                        border-radius: 10px;
                        pointer-events: none;
                        transition-duration: 0.3s;

                        animation: asdfsdsadf 0.3s forwards ease-in-out;

                        @keyframes asdfsdsadf {
                            from { width: 0 }
                            to {  width: 100% }
                        }
                    }
                }

                &:hover {
                    &::after {
                        content: "";
                        display: block;
                        width: 100%;
                        height: 2px;
                        background: var(--accent-color-secondary);
                        margin-top: 5px;
                        position: absolute;
                        border-radius: 10px;
                        animation: asdfsdf 0.3s forwards ease-in-out;
                        pointer-events: none;
                        transition-duration: 0.3s;

                        @keyframes asdfsdf {
                            from { width: 0 }
                            to {  width: 100% }
                        }
                    }
                }
            }
        }
    }

    >.smallDevice{
        display: none;
        width: 50px;
        height: 50px;
        position: absolute;
        right: 5%;
        top: 50%;
        transform: translateY(-50%);
        mask-image: url(../../public/images/svg/menu.svg);
        mask-size: 90%;
        mask-position: center;
        mask-repeat: no-repeat;
        background-color: var(--text-color);
        cursor: pointer;
        transition-duration: 0.3s;

        &:hover {
            background-color: var(--color-gray) !important;
            transition-duration: 0.3s;
        }
    }

    >.Header-Logo-Small {
        display: none;
        height: 50px;
        width: 50px;
        mask-image: var(--tda-logo-small);
        mask-size: contain;
        mask-repeat: no-repeat;
        mask-position: center;
        background-color: var(--text-color);
        top: 50%;
        transform: translateY(-50%);
        position: absolute;
        left: 5%;
        cursor: pointer;
    }



    &:is(.scrolled) {
        background: var(--background-color-3);

        >.flex{
            >.Login{
                >.User{
                    .UserIcon{
                        color: var(--text-color-primary);
                    }
                    .Texts{
                        >.top,
                        >.bot{
                            p{
                                color : var(--text-color-primary);
                            }
                        }
                    }
                }
            }
        }

        .newgame {
            background-color: var(--accent-color-primary) !important;
            transition-duration: 0.3s;

            &:hover {
                background-color: var(--accent-color-primary-darker) !important;
            }
        }
    }


}

@media screen and (min-width: 600px) and (max-width: 960px) {
    .header-5F015D44-0984-4A50-B52B-5319AE57C19C {
        >.flex {
            display: none;
        }
        >.smallDevice {
            display: unset;
        }
        >.Header-Logo-Small {
            display: grid;
        }
    }
}

@media screen and (max-width: 600px) {
    header{

        >.flex {
            display: none;
        }
        >.smallDevice {
            display: unset;
        }
        >.Header-Logo-Small {
            display: grid;
        }
    }
}
</style>
