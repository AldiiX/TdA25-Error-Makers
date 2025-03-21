import { createApp, ref, onMounted } from 'vue'
import App from './App.vue'
import { createWebHistory, createRouter } from 'vue-router'
import './main.css'
import Home from "@/pages/Home.vue";
import About from "@/pages/About.vue";
import Error from "@/pages/Error.vue";
import GDPR from "@/pages/GDPR.vue";
import Cookies from "@/pages/Cookies.vue";
import Features from "@/pages/Features.vue";
import Chat from "@/pages/Chat.vue";



// cookies metody
const setCookie = (name: string, value: string, days: number) => {
  let expires = "";
  if (days) {
    const date = new Date();
    date.setTime(date.getTime() + (days * 24 * 60 * 60 * 1000));
    expires = "; expires=" + date.toUTCString();
  }
  document.cookie = name + "=" + (value || "")  + expires + "; path=/";
}

const getCookie = (name: string) => {
  const nameEQ = name + "=";
  const ca = document.cookie.split(';');
  for(let i=0;i < ca.length;i++) {
    let c = ca[i];
    while (c.charAt(0) === ' ') c = c.substring(1,c.length);
    if (c.indexOf(nameEQ) === 0) return c.substring(nameEQ.length,c.length);
  }
  return null;
}







// theme
const theme = ref<string>('light');

export const toggleTheme = () => {
  theme.value = theme.value === 'light' ? 'dark' : 'light';
  const root = document.documentElement;

  root.classList.remove('theme-light', 'theme-dark');
  root.classList.add("theme-" + theme.value);

    setCookie('theme', theme.value, 365);
}

const setTheme = (newTheme: string) => {
    theme.value = newTheme;
    setCookie('theme', theme.value, 365);

    const root = document.documentElement;
    root.classList.remove('light', 'dark');
    root.classList.add("theme-" + theme.value);
}

export const getTheme = () => {
    const savedTheme = getCookie('theme');
    if (savedTheme) setTheme(savedTheme);
    else setTheme('light');
}



const routes = [
    {
        path: '/',
        name: 'Chat',
        component: Chat,
        meta: {
            title: "Chat",
            //hasColoredBackground: true,
        }
    },
    {
        path: '/home',
        name: 'Home',
        component: Home,
        meta: {
            title: "Domů",
            hasColoredBackground: true,
        }
    },
    {
        path: '/features',
        name: 'Features',
        component: Features,

    },
    {
        path: '/:pathMatch(.*)*',
        name: 'Error',
        props: { code: 404 },
        component: Error,
        meta: {
            title: "Chyba 404",
            icon: "/images/svg/favicon_error.svg",
            hasColoredBackground: false,
        }
    },
    { path: '/privacy/gdpr', name: "GDPR", component: GDPR, meta: { hasColoredBackground: true } },
    { path: '/privacy/cookies', name: "Cookies", component: Cookies, meta: { hasColoredBackground: true } },
]

const router = createRouter({
    history: createWebHistory(),
    routes,
    linkActiveClass: 'active',
    linkExactActiveClass: 'active',
})





// pri zmeneni routu se nastavi promenna isTransitioning na true, po 100ms se nastavi na false
const isTransitioning = ref<boolean>(false);
const transitionType = ref<'blurbg' | 'coloredbg'>('blurbg');
const transitionEnabled = ref<boolean>(false);

router.beforeEach((to, from, next) => {
    window.scrollTo({ top: 0, behavior: 'smooth' });

    // nastavení titlu
    document.title = (to.meta.title as string ?? to.name as string) + ' • Think Different Academy';

    // nastavení ikony
    const favicon = document.querySelector('link[rel="icon"]');
    if (to.meta.icon) favicon?.setAttribute('href', to.meta.icon as string);
    else favicon?.setAttribute('href', '/favicon.ico');

    if(!transitionEnabled.value) {
        next();
        return;
    }

    isTransitioning.value = true;
    transitionType.value = to.meta.hasColoredBackground ? 'coloredbg' : 'blurbg';

    setTimeout(() => {
        next();
    }, 150);
});




router.afterEach(() => {
    if(!transitionEnabled.value) {
        transitionEnabled.value = true;
        return;
    }

    const transitionDiv = document.getElementById("transition-div");
    window.scrollTo(0, 0);

    setTimeout(() => {
        transitionDiv?.classList.add("fade-out");
    }, 150);

    setTimeout(() => {
        transitionDiv?.classList.remove("fade-out");
        isTransitioning.value = false;
    }, 300);
});



const app = createApp(App);
app.provide('theme', theme);
app.provide('isTransitioning', isTransitioning);
app.provide('transitionType', transitionType);
app.use(router);
app.mount('#app');
