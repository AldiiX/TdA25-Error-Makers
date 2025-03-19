<script setup lang="ts">
    import { RouterLink } from 'vue-router';
</script>

<template>
    <footer class="footer-3AEC085F-28D0-4E88-B74C-1ED2EB5A13E1">
        <div class="divider"></div>
        <div class="content">
            <div class="Header-Main-Section">
                <p class="text">Think different Academy</p>
                <div class="Icons">
                    <div class="x"></div>
                    <div class="o"></div>
                    <div class="x"></div>
                </div>
            </div>

            <div class="content-flex">
                <div class="navigation">
                    <p class="upper">Navigace</p>
                    <p>
                        <RouterLink to="/">Domů</RouterLink>
                    </p>
                    <p>
                        <RouterLink to="/leaderboard">Žebříčky</RouterLink>
                    </p>
                    <p>
                        <RouterLink to="/account">Účet</RouterLink>
                    </p>
                    <p>
                        <RouterLink to="/play">Hrát</RouterLink>
                    </p>
                </div>

                <div class="other">
                    <p class="upper">Ostatní</p>
                    <p>
                        <RouterLink to="/privacy/gdpr">GDPR</RouterLink>
                    </p>
                    <p>
                        <RouterLink to="/privacy/cookies">Cookies</RouterLink>
                    </p>
                </div>

                <div class="team">
                    <p class="upper">Tým</p>
                    <p>
                        Fullstack:&nbsp;&nbsp;&nbsp;&nbsp;
                        <a href="https://stanislavskudrna.cz" target="_blank" style="--team-image: url(/images/team/standa.png)"> Stanislav Škudrna</a>
                    </p>
                    <p>
                        Frontend:&nbsp;&nbsp;&nbsp;&nbsp;
                        <a href="https://github.com/dgkuba657" target="_blank" style="--team-image: url(/images/team/kuba.png)"> Jakub Funk</a>
                    </p>
                    <p>
                        Frontend:&nbsp;&nbsp;&nbsp;&nbsp;
                        <a href="https://github.com/wedralda" target="_blank" style="--team-image: url(/images/team/paja.png)"> Pavel Andrle</a>
                    </p>
                </div>

<!--                <div class="login">-->

<!--                </div>-->
            </div>

            <div class="copyright">
                <div class="copyright-tda">
                    <span>©</span>
                    Think different Academy
                    <span>2024-2025</span>
                </div>
                <div class="copyright-team">
                    Vytvořeno týmem
                    Error Makers
                    ze školy
                    <a href="https://educhem.cz" target="_blank">Střední škola EDUCHEM, a.s.</a>
                    pro
                    <a href="https://tourde.app/" target="_blank">Tour de App</a>.
                </div>
            </div>
        </div>
    </footer>
</template>

<style scoped>
@keyframes adsadsadsasa {
    0% { transform: translateX(50vw); opacity: 0;  }
    80% { transform: translateX(50vw); opacity: 0; }
    100% { transform: translateX(0px); opacity: 1;  }
}

.footer-3AEC085F-28D0-4E88-B74C-1ED2EB5A13E1 {
    background-color: var(--background-color-3);
    color: var(--text-color);
    padding: 48px 0;
    position: relative;
    font-family: Dosis, sans-serif;
    transition-duration: 0.3s;

    .divider {
        position: absolute;
        bottom: calc(100% - 2px);
        width: 100%;
        height: 80px;
        background-image: var(--footer-divider);
        background-repeat: repeat-x;
        background-position: bottom;
        transition-duration: 0.3s;
    }

    .content{
        .Header-Main-Section{
            display: flex;
            justify-content: space-between;
            width: 75vw;
            margin: 0 auto;

            .text{
                font-size: 28px;
                font-weight: 1000;
            }

            .Icons{
                display: flex;
                align-items: center;
                .x{
                    width: 20px;
                    height: 20px;
                    background-image: var(--tda-icon-x);
                }
                .o{
                    width: 20px;
                    height: 20px;
                    background-image: var(--tda-icon-o);
                }
            }
        }

        .content-flex{
            display: flex;
            justify-content: space-between;
            margin: 48px auto;
            width: 75vw;

            p {
                margin: 0;
            }

            >.navigation,
            >.other,
            >.login,
            >.admintools,
            >.team {
                p:not(.upper) {
                    margin: 10px 0;
                    text-align: left;

                    & a:is(.active) {
                        color: var(--color-gray);
                    }
                }

                .upper{
                    font-size: 24px;
                    font-weight: 700;
                    margin-bottom: 20px;
                    border-bottom: 1px solid;
                    width: fit-content;
                    border-bottom-color: var(--text-color-primary);
                    pointer-events: none;
                }

                a {
                    color: var(--text-color-primary);
                    text-decoration: none;
                    margin: 10px 0;
                    transition-duration: 0.3s;

                    &:hover{
                        color: var(--accent-color-primary);
                        transition-duration: 0.3s;
                    }
                }

                span{
                    color: var(--accent-color-primary);
                    transition-duration: 0.3s;
                }

                button {
                    display: grid;
                }
            }

            >.team {
                >p >a::after {
                    width: 600px;
                    height: 600px;
                    content: '';
                    bottom: 0;
                    position: absolute;
                    display: none;
                    pointer-events: none;
                    background-image: var(--team-image);
                    background-size: contain;
                    background-repeat: no-repeat;
                    background-position: center;
                    opacity: 0;
                    transition-duration: 0.3s;
                    animation: adsadsadsasa 1.75s 1;
                }

                >p >a:active::after {
                    display: grid;
                    opacity: 1;
                    transition-duration: 0.3s;
                }
            }

            .login{
                .UserInformation{
                    display: flex;
                    align-items: center;
                    gap: 8px;
                    margin-bottom: 24px;
                    user-select: none;
                    cursor: pointer;
                    transition-duration: 0.3s;

                    &:hover {
                        opacity: 0.5;
                        transition-duration: 0.3s;
                    }

                    .UserIcon{
                        font-size: 24px;
                        font-weight: 700;
                        color: var(--text-color-primary);
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
                .logout{
                    position: relative;
                    text-align: left;
                    margin: 0;
                }
            }
        }

        .copyright{
            display: flex;
            font-size: 12px;

            .copyright-tda{
                text-align: left;
                font-weight: 500;
                margin-left: 12vw;
                margin-top: 10px;

                span{
                    color: var(--text-color-primary);
                    text-align: center;
                    font-size: 12px;
                }
            }

            .copyright-team{
                font-weight: 500;
                text-align: right;
                margin-left: auto;
                margin-right: 12vw;
                margin-top: 10px;

                a {
                    color: var(--text-color-primary);
                    text-decoration: none;
                    font-size: 12px;
                    transition-duration: 0.3s;

                    &:hover{
                        color: var(--accent-color-secondary);
                        transition-duration: 0.3s;
                    }
                }

                span {
                    color: var(--accent-color-secondary);
                    text-decoration: none;
                }
            }

        }
    }
}

@media screen and (max-width: 600px) {
    .footer-3AEC085F-28D0-4E88-B74C-1ED2EB5A13E1{
        .divider {
            width: 300%;
        }

        >.content{
            >.content-flex {
                display: grid;
                grid-template-columns: auto auto;
                gap: 24px;

                >div:nth-child(even) {
                    >.upper {
                        text-align: right;
                        margin-left: auto;
                    }

                    >p {
                        text-align: right;

                        >a {
                            text-align: right;
                            margin: 0;
                            display: grid;
                            width: 100%;
                        }
                    }

                    .UserInformation {
                        flex-direction: row-reverse;
                        gap: 8px;
                        margin-bottom: 24px;

                        p {
                            text-align: right;
                        }
                    }

                    .logout {
                        text-align: right;
                        margin-left: auto;
                        display: grid;
                    }
                }

                > .team > p > a:active::after {
                    display: none !important;
                }
            }

            >.copyright{
                flex-direction: column;
                width: 75vw;
                gap: 10px;
                margin: 0 auto;

                >.copyright-tda,
                >.copyright-team{
                    text-align: justify;
                    margin-left: 0;
                    margin-right: 0;
                    margin-top: 0;
                }
            }
        }
    }
}
</style>
