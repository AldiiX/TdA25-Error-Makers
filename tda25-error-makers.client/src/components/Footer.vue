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

<style scoped src="./Footer.scss">

</style>
