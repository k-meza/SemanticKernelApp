<!-- src/App.vue -->
<script setup lang="ts">
</script>

<template>
  <header style="padding: 12px; border-bottom: 1px solid #eee;">
    <nav style="display:flex; gap:12px;">
      <RouterLink to="/">Home</RouterLink>
      <RouterLink to="/login">Login</RouterLink> <!-- handy link while developing -->
      <RouterLink to="/chat">Chat</RouterLink> <!-- handy link while developing -->
      <RouterLink to="/vectorization">Vectorization</RouterLink>
    </nav>
  </header>

  <main style="padding: 16px;">
    <RouterView />  <!-- This is where LoginView renders when you go to /login -->
  </main>
</template>
