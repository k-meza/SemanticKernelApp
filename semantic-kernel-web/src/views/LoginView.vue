<!-- src/views/LoginView.vue -->
<script setup lang="ts">
import { Authenticator } from '@aws-amplify/ui-vue';
import '@aws-amplify/ui-vue/styles.css';
</script>

<template>
  <authenticator initial-state="signIn">
    <!-- use the DEFAULT slot, not #authenticated -->
    <template v-slot="{ user, signOut }">
      <div class="p-4">
        <h2>Welcome {{ user?.username }}</h2>
        <button @click="signOut">Sign out</button>
      </div>
    </template>
  </authenticator>
</template>
