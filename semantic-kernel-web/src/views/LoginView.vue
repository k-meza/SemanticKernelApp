<!-- src/views/LoginView.vue -->
<script setup lang="ts">
import { Authenticator } from '@aws-amplify/ui-vue';
import '@aws-amplify/ui-vue/styles.css';
import { useRouter, useRoute } from 'vue-router';
import { onMounted, onUnmounted, watch, ref } from 'vue';
import { Hub } from 'aws-amplify/utils';
import { getCurrentUser } from 'aws-amplify/auth';

const router = useRouter();
const route = useRoute();
const isAuthenticated = ref(false);
const countdown = ref(0);
const isRedirecting = ref(false);

let hubListener: (() => void) | null = null;
let countdownInterval: NodeJS.Timeout | null = null;

// Function to start countdown and redirect
const startRedirectCountdown = () => {
  if (isRedirecting.value) return; // Prevent multiple countdowns

  isRedirecting.value = true;
  countdown.value = 5;

  countdownInterval = setInterval(() => {
    countdown.value--;

    if (countdown.value <= -1) {
      if (countdownInterval) {
        clearInterval(countdownInterval);
        countdownInterval = null;
      }

      const redirectPath = (route.query.redirect as string) || '/';
      router.push(redirectPath);
    }
  }, 1000);
};

// Check authentication status
const checkAuthStatus = async () => {
  try {
    await getCurrentUser();
    isAuthenticated.value = true;
    // Start countdown instead of immediate redirect
    setTimeout(startRedirectCountdown, 500);
  } catch {
    isAuthenticated.value = false;
  }
};

onMounted(() => {
  // Initial auth check
  checkAuthStatus();

  // Listen to auth events using Hub
  hubListener = Hub.listen('auth', async (data) => {
    console.log('Auth event:', data.payload); // Debug log
    const { event } = data.payload;

    // Handle various auth events - fix TypeScript error by checking valid event types
    if (event === 'signedIn' || event === 'signInWithRedirect' || event === 'signInWithRedirect_failure') {
      // Wait a bit for auth state to settle
      setTimeout(async () => {
        try {
          await getCurrentUser();
          startRedirectCountdown();
        } catch {
          // If getCurrentUser fails, try again after a short delay
          setTimeout(checkAuthStatus, 1000);
        }
      }, 100);
    }
  });
});

onUnmounted(() => {
  if (hubListener) {
    hubListener();
  }
  if (countdownInterval) {
    clearInterval(countdownInterval);
  }
});

// Watch for changes in authentication status
watch(isAuthenticated, (newVal) => {
  if (newVal && !isRedirecting.value) {
    startRedirectCountdown();
  }
});

// Function to cancel redirect
const cancelRedirect = () => {
  if (countdownInterval) {
    clearInterval(countdownInterval);
    countdownInterval = null;
  }
  isRedirecting.value = false;
  countdown.value = 0;
};
</script>

<template>
  <div class="login-container">
    <div class="login-content">
      <authenticator
        initial-state="signIn"
        :key="isAuthenticated ? 'authenticated' : 'unauthenticated'"
      >
        <template v-slot="{ user, signOut }">
          <!-- This template will be shown when user is already authenticated -->
          <div class="authenticated-content">
            <h2>Welcome {{ user?.username }}!</h2>

            <div v-if="isRedirecting" class="redirect-content">
              <p>Redirecting to the app in:</p>
              <div class="countdown-circle">
                <div class="countdown-number">{{ countdown }}</div>
                <svg class="countdown-progress" width="80" height="80">
                  <circle
                    cx="40"
                    cy="40"
                    r="35"
                    fill="none"
                    stroke="#e0e0e0"
                    stroke-width="4"
                  />
                  <circle
                    cx="40"
                    cy="40"
                    r="35"
                    fill="none"
                    stroke="#667eea"
                    stroke-width="4"
                    stroke-linecap="round"
                    :stroke-dasharray="220"
                    :stroke-dashoffset="220 - (220 * (5 - countdown) / 5)"
                    transform="rotate(-90 40 40)"
                  />
                </svg>
              </div>

              <div class="redirect-actions">
                <button @click="cancelRedirect" class="btn btn-secondary">
                  Cancel
                </button>
                <button @click="startRedirectCountdown" class="btn btn-primary">
                  Redirect Now
                </button>
              </div>
            </div>

            <div v-else class="auth-actions">
              <p>You're already signed in!</p>
              <div class="action-buttons">
                <button @click="startRedirectCountdown" class="btn btn-primary">
                  Go to App
                </button>
                <button @click="signOut" class="btn btn-outline">
                  Sign out
                </button>
              </div>
            </div>
          </div>
        </template>
      </authenticator>
    </div>
  </div>
</template>

<style scoped>
.login-container {
  min-height: 100vh;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 2rem;
}

.login-content {
  background: white;
  border-radius: 16px;
  box-shadow: 0 20px 40px rgba(0, 0, 0, 0.1);
  padding: 2rem;
  max-width: 500px;
  width: 100%;
}

.authenticated-content {
  text-align: center;
  padding: 2rem;
}

.authenticated-content h2 {
  margin-bottom: 2rem;
  color: #333;
  font-size: 1.8rem;
}

.redirect-content p {
  margin-bottom: 2rem;
  color: #666;
  font-size: 1.1rem;
}

.countdown-circle {
  position: relative;
  display: inline-block;
  margin-bottom: 2rem;
}

.countdown-number {
  position: absolute;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
  font-size: 2rem;
  font-weight: bold;
  color: #667eea;
}

.countdown-progress {
  transform: rotate(-90deg);
}

.countdown-progress circle:last-child {
  transition: stroke-dashoffset 1s linear;
}

.redirect-actions {
  display: flex;
  gap: 1rem;
  justify-content: center;
  flex-wrap: wrap;
}

.auth-actions {
  display: flex;
  flex-direction: column;
  gap: 2rem;
}

.auth-actions p {
  color: #666;
  font-size: 1.1rem;
  margin: 0;
}

.action-buttons {
  display: flex;
  gap: 1rem;
  justify-content: center;
  flex-wrap: wrap;
}

.btn {
  padding: 12px 24px;
  border-radius: 8px;
  font-size: 1rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s ease;
  border: 2px solid transparent;
  text-decoration: none;
  display: inline-block;
  min-width: 120px;
}

.btn-primary {
  background: #667eea;
  color: white;
  border-color: #667eea;
}

.btn-primary:hover {
  background: #5a6fd8;
  border-color: #5a6fd8;
  transform: translateY(-2px);
  box-shadow: 0 5px 15px rgba(102, 126, 234, 0.3);
}

.btn-secondary {
  background: #6c757d;
  color: white;
  border-color: #6c757d;
}

.btn-secondary:hover {
  background: #5a6268;
  border-color: #545b62;
  transform: translateY(-2px);
  box-shadow: 0 5px 15px rgba(108, 117, 125, 0.3);
}

.btn-outline {
  background: transparent;
  border-color: #667eea;
  color: #667eea;
}

.btn-outline:hover {
  background: #667eea;
  color: white;
  transform: translateY(-2px);
  box-shadow: 0 5px 15px rgba(102, 126, 234, 0.3);
}

@media (max-width: 768px) {
  .login-container {
    padding: 1rem;
  }

  .login-content {
    padding: 1.5rem;
  }

  .authenticated-content {
    padding: 1.5rem;
  }

  .redirect-actions,
  .action-buttons {
    flex-direction: column;
    align-items: center;
  }

  .btn {
    width: 100%;
    max-width: 200px;
  }
}
</style>
