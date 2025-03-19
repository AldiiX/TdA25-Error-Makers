<script setup lang="ts">

import ColoredBackground from "@/components/backgrounds/ColoredBackground.vue";
</script>

<template>
    <div class="sections">
        <section class="top">
            <ColoredBackground v-bind:positionAbsolute="true" />

            <div class="texts">
                <h1>GDPR</h1>
            </div>
        </section>
        <section class="mainsection">
            <div class="texts">
                <p>Aplikace Piškvorky respektuje zásady Obecného nařízení o ochraně osobních údajů (GDPR) a dbá na ochranu soukromí svých uživatelů.</p>
                <p>Při přihlašování aplikace shromažďuje pouze nezbytné údaje, jako je uživatelské jméno a heslo, které slouží výhradně pro účely autentifikace uživatele. Tyto údaje jsou bezpečně uloženy a zpracovávány v souladu s GDPR a nejsou sdíleny s žádnými třetími stranami.</p>
                <p>Aplikace neumožňuje uživatelům zobrazení nebo úpravu jejich údajů prostřednictvím rozhraní. Uživatelé nemají přístup k jejich údajům v aplikaci, ale mohou požádat o odstranění svého účtu nebo údajů prostřednictvím kontaktování správce aplikace. V takovém případě budou jejich údaje vymazány v souladu s požadavky GDPR.</p>
                <p>Tento přístup zajistí, že aplikace Piškvorky splňuje požadavky GDPR a chrání soukromí svých uživatelů.</p>
            </div>
        </section>
    </div>
</template>

<style scoped src="./GDPR.scss">

</style>
