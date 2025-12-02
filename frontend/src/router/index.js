import { createRouter, createWebHistory } from 'vue-router'
import { authService } from '../services/authService'
import LoginView from '../views/LoginView.vue'
import GamesView from '../views/GamesView.vue'
import GameView from '../views/GameView.vue'
import HomeView from '../views/HomeView.vue'
import PlayBotView from '../views/PlayBotView.vue'

const routes = [
  {
    path: '/',
    redirect: '/home'
  },
  {
    path: '/home',
    name: 'Home',
    component: HomeView,
    meta: { requiresAuth: true }
  },
  {
    path: '/login',
    name: 'Login',
    component: LoginView,
    meta: { requiresGuest: true }
  },
  {
    path: '/games',
    name: 'Games',
    component: GamesView,
    meta: { requiresAuth: true }
  },
  {
    path: '/game/:gameId',
    name: 'Game',
    component: GameView,
    props: true,
    meta: { requiresAuth: true }
  },
  {
    path: '/play/bot',
    name: 'PlayBot',
    component: PlayBotView,
    meta: { requiresAuth: true }
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

// Provide router to authService for automatic redirects on 401 errors
authService.setRouter(router)

// Navigation guards
router.beforeEach((to, from, next) => {
  const isAuthenticated = authService.isAuthenticated()

  if (to.meta.requiresAuth && !isAuthenticated) {
    next('/login')
  } else if (to.meta.requiresGuest && isAuthenticated) {
    next('/games')
  } else {
    next()
  }
})

export default router