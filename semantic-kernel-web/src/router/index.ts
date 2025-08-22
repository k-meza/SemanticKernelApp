// src/router/index.ts
import { createRouter, createWebHistory } from 'vue-router';

// Import your pages/views
import LoginView from '../views/LoginView.vue';
import HomeView from '../views/HomeView.vue';
import ChatView from '../views/ChatView.vue';
import { getCurrentUser } from 'aws-amplify/auth';

const routes = [
  { path: '/login', name: 'login', component: LoginView },
  { path: '/', name: 'home', component: HomeView, meta: { requiresAuth: true } },
  { path: '/chat', name: 'chat', component: ChatView, meta: { requiresAuth: true } },
];

const router = createRouter({
  history: createWebHistory(), // nice-looking URLs
  routes,
});

router.beforeEach(async (to) => {
  if (to.meta?.requiresAuth) {
    try {
      await getCurrentUser();
      // user is authenticated, allow navigation
      return true;
    } catch {
      // not authenticated, redirect to login
      return { path: '/login', query: { redirect: to.fullPath } };
    }
  }
  return true;
});

export default router;
