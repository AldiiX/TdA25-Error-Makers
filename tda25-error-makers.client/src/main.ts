import { createApp } from 'vue'
import App from './App.vue'
import { createWebHistory, createRouter } from 'vue-router'
import Home from './pages/Home.vue'
import About from './pages/About.vue'
import './main.css'


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
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes,
})



const app = createApp(App)
app.use(router)
app.mount('#app')
