<script setup lang="ts">
import {inject, type Ref, ref} from 'vue';
import { type LoggedUser } from "../main.ts";
import axios from 'axios';
import BlurBackground from "@/components/backgrounds/BlurBackground.vue";
import {useRoute, useRouter} from "vue-router";

const router = useRoute();
const loggedUser = inject("loggedUser") as Ref<LoggedUser | any | null>;


const username = ref('');
const password = ref('');


async function submitForm() {
    if (!username.value || !password.value) {
        alert('Vyplňte obě pole prosím.');
        return;
    }


    const formData = {
        username: username.value,
        password: password.value,
    };

    try {

        const response = await axios.post('/api/v1/loggeduser', formData);

        if (response != null) {
            console.log('Login successful', response.data);
            loggedUser.value = response;
            console.log(loggedUser.value);
        } else {
            alert('Neplatné jméno nebo heslo');
        }
    } catch (error) {
        console.error('Chyba během login requestu', error);
        alert('Nastala chyba, zkuste to později.');
    }
}
</script>

<template>
    <BlurBackground />

    <div class="sections">
        <section class="mainsection">
            <div class="auth-box">
                <h1>Login</h1>
                <p>Nemáš účet? <a href="/register">Registruj se!</a></p>
                <form @submit.prevent="submitForm">
                    <div class="input">
                        <label for="username">Uživatelské jméno nebo Email:</label>
                        <input
                            type="text"
                            id="username"
                            name="username"
                            v-model="username"
                            required
                        />
                    </div>
                    <div class="input">
                        <label for="password">Heslo:</label>
                        <input
                            type="password"
                            id="password"
                            name="password"
                            v-model="password"
                            required
                        />
                    </div>
                    <div class="submit">
                        <input type="submit" class="button-primary" value="Přihlásit se" />
                    </div>
                </form>
            </div>
        </section>
    </div>
</template>

<style scoped src="./Login.scss">
</style>
