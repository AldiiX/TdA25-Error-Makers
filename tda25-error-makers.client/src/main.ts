import { createApp, ref, onMounted } from 'vue'
import App from './App.vue'
import { createWebHistory, createRouter } from 'vue-router'
import './main.css'
import Home from "@/pages/Home.vue";
import About from "@/pages/About.vue";
import Error from "@/pages/Error.vue";



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
        name: 'Home',
        component: Home
    },
    {
        path: '/about',
        name: 'About',
        component: About
    },
    {
        path: '/:pathMatch(.*)*',
        name: 'Error',
        props: { code: 404 },
        component: Error
    }
]

const router = createRouter({
    history: createWebHistory(),
    routes,
    linkActiveClass: 'active',
    linkExactActiveClass: 'active',
})

router.beforeEach((to, from, next) => {
    window.scrollTo({ top: 0, behavior: 'smooth' });
    next();
});



const app = createApp(App)
app.use(router)
app.mount('#app')
app.provide('theme', theme)
