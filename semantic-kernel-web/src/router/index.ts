// src/router/index.ts
import { createRouter, createWebHistory } from 'vue-router';

// Import your pages/views
import LoginView from '../views/LoginView.vue';

const routes = [
  { path: '/login', name: 'login', component: LoginView },  // <-- your login view
];

const router = createRouter({
  history: createWebHistory(), // nice-looking URLs
  routes,
});

export default router;
