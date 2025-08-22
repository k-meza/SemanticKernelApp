<!-- src/views/HomeView.vue -->
<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { getCurrentUser, signOut } from 'aws-amplify/auth';
import { useRouter } from 'vue-router';

interface SignInDetails {
  loginId?: string;
}

const username = ref<string | null>(null);
const email = ref<string | null>(null);
const loading = ref(true);
const error = ref<string | null>(null);

const router = useRouter();

onMounted(async () => {
  try {
    const user = await getCurrentUser();
    username.value = user.username ?? null;
    const signInDetails = user?.signInDetails as SignInDetails | undefined;
    email.value = signInDetails?.loginId ?? username.value;
  } catch {
    error.value = 'Failed to load user information.';
  } finally {
    loading.value = false;
  }
});

async function handleSignOut() {
  try {
    await signOut();
    await router.push('/login');
  } catch {
    error.value = 'Failed to sign out.';
  }
}

function goToChat() {
  router.push('/chat');
}
</script>

<template>
  <div class="page">
    <div class="container">
      <!-- Header -->
      <header class="header">
        <div class="header-content">
          <h1 class="logo">Dashboard</h1>
          <button @click="handleSignOut" class="logout-btn">
            Sign Out
          </button>
        </div>
      </header>

      <!-- Main Content -->
      <main class="main">
        <!-- Loading State -->
        <div v-if="loading" class="loading-state">
          <div class="spinner"></div>
          <p>Loading your profile...</p>
        </div>

        <!-- Error State -->
        <div v-else-if="error" class="error-state">
          <div class="error-icon">⚠️</div>
          <h2>Something went wrong</h2>
          <p>{{ error }}</p>
          <button @click="handleSignOut" class="retry-btn">
            Try Again
          </button>
        </div>

        <!-- Success State -->
        <div v-else class="dashboard">
          <!-- Welcome Section -->
          <section class="welcome">
            <div class="welcome-content">
              <h2 class="welcome-title">
                Welcome back{{ username ? `, ${username}` : '' }}! 👋
              </h2>
              <p v-if="email" class="welcome-subtitle">
                {{ email }}
              </p>
            </div>
          </section>

          <!-- Dashboard Grid -->
          <section class="dashboard-grid">
            <!-- Stats Card -->
            <div class="card stats-card">
              <div class="card-header">
                <h3>Quick Stats</h3>
                <div class="stats-icon">📊</div>
              </div>
              <div class="card-content">
                <div class="stat-item">
                  <span class="stat-label">Status</span>
                  <span class="stat-value active">Active</span>
                </div>
                <div class="stat-item">
                  <span class="stat-label">Last Login</span>
                  <span class="stat-value">{{ new Date().toLocaleDateString() }}</span>
                </div>
              </div>
            </div>

            <!-- Actions Card -->
            <div class="card actions-card">
              <div class="card-header">
                <h3>Quick Actions</h3>
                <div class="actions-icon">⚡</div>
              </div>
              <div class="card-content">
                <button @click="goToChat" class="action-btn primary">
                  <span class="btn-icon">💬</span>
                  <span class="btn-text">Join Chat</span>
                  <span class="btn-arrow">→</span>
                </button>
                <button class="action-btn secondary" disabled>
                  <span class="btn-icon">📋</span>
                  <span class="btn-text">View Reports</span>
                  <span class="btn-badge">Soon</span>
                </button>
                <button class="action-btn secondary" disabled>
                  <span class="btn-icon">⚙️</span>
                  <span class="btn-text">Settings</span>
                  <span class="btn-badge">Soon</span>
                </button>
              </div>
            </div>

            <!-- Activity Card -->
            <div class="card activity-card">
              <div class="card-header">
                <h3>Recent Activity</h3>
                <div class="activity-icon">🕒</div>
              </div>
              <div class="card-content">
                <div class="activity-item">
                  <div class="activity-dot"></div>
                  <div class="activity-text">
                    <span class="activity-action">Signed in</span>
                    <span class="activity-time">Just now</span>
                  </div>
                </div>
                <div class="activity-item">
                  <div class="activity-dot inactive"></div>
                  <div class="activity-text">
                    <span class="activity-action">Profile updated</span>
                    <span class="activity-time">2 days ago</span>
                  </div>
                </div>
              </div>
            </div>
          </section>
        </div>
      </main>
    </div>
  </div>
</template>

<style scoped>
/* Reset and Base Styles */
* {
  box-sizing: border-box;
}

.page {
  min-height: 100vh;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  padding: 1rem;
}

.container {
  max-width: 1200px;
  margin: 0 auto;
  width: 100%;
}

/* Header */
.header {
  background: rgba(255, 255, 255, 0.1);
  backdrop-filter: blur(10px);
  border-radius: 16px;
  margin-bottom: 2rem;
  border: 1px solid rgba(255, 255, 255, 0.2);
}

.header-content {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1.5rem 2rem;
}

.logo {
  font-size: 1.5rem;
  font-weight: 700;
  color: white;
  margin: 0;
}

.logout-btn {
  background: rgba(255, 255, 255, 0.2);
  border: 1px solid rgba(255, 255, 255, 0.3);
  color: white;
  padding: 0.5rem 1rem;
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.3s ease;
  font-weight: 500;
}

.logout-btn:hover {
  background: rgba(255, 255, 255, 0.3);
  transform: translateY(-1px);
}

/* Main Content */
.main {
  background: white;
  border-radius: 20px;
  box-shadow: 0 20px 40px rgba(0, 0, 0, 0.1);
  overflow: hidden;
}

/* Loading State */
.loading-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 4rem 2rem;
  gap: 1rem;
}

.spinner {
  width: 48px;
  height: 48px;
  border: 4px solid #f3f3f3;
  border-top: 4px solid #667eea;
  border-radius: 50%;
  animation: spin 1s linear infinite;
}

@keyframes spin {
  0% { transform: rotate(0deg); }
  100% { transform: rotate(360deg); }
}

.loading-state p {
  color: #666;
  font-size: 1.1rem;
}

/* Error State */
.error-state {
  text-align: center;
  padding: 4rem 2rem;
}

.error-icon {
  font-size: 3rem;
  margin-bottom: 1rem;
}

.error-state h2 {
  color: #dc3545;
  margin-bottom: 1rem;
}

.error-state p {
  color: #666;
  margin-bottom: 2rem;
}

.retry-btn {
  background: #6c757d;
  color: white;
  border: none;
  padding: 0.75rem 1.5rem;
  border-radius: 8px;
  cursor: pointer;
  font-weight: 600;
  transition: all 0.3s ease;
}

.retry-btn:hover {
  background: #5a6268;
  transform: translateY(-2px);
}

/* Dashboard */
.dashboard {
  padding: 2rem;
}

/* Welcome Section */
.welcome {
  text-align: center;
  margin-bottom: 3rem;
}

.welcome-content {
  max-width: 600px;
  margin: 0 auto;
}

.welcome-title {
  font-size: 2.5rem;
  font-weight: 700;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
  margin-bottom: 0.5rem;
  line-height: 1.2;
}

.welcome-subtitle {
  font-size: 1.1rem;
  color: #666;
  margin: 0;
}

/* Dashboard Grid */
.dashboard-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(320px, 1fr));
  gap: 2rem;
  align-items: start;
}

/* Cards */
.card {
  background: #f8f9fa;
  border-radius: 16px;
  border: 1px solid #e9ecef;
  overflow: hidden;
  transition: all 0.3s ease;
}

.card:hover {
  transform: translateY(-4px);
  box-shadow: 0 8px 25px rgba(0, 0, 0, 0.1);
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1.5rem 2rem;
  border-bottom: 1px solid #e9ecef;
  background: white;
}

.card-header h3 {
  margin: 0;
  font-size: 1.25rem;
  font-weight: 600;
  color: #333;
}

.card-content {
  padding: 2rem;
}

/* Stats Card */
.stats-icon {
  font-size: 1.5rem;
}

.stat-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1rem;
}

.stat-item:last-child {
  margin-bottom: 0;
}

.stat-label {
  color: #666;
  font-weight: 500;
}

.stat-value {
  font-weight: 600;
  color: #333;
}

.stat-value.active {
  color: #28a745;
}

/* Actions Card */
.actions-icon {
  font-size: 1.5rem;
}

.action-btn {
  width: 100%;
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 1rem 1.5rem;
  border: 2px solid transparent;
  border-radius: 12px;
  background: white;
  cursor: pointer;
  transition: all 0.3s ease;
  margin-bottom: 1rem;
  font-size: 1rem;
  font-weight: 500;
  text-align: left;
}

.action-btn:last-child {
  margin-bottom: 0;
}

.action-btn.primary {
  background: #667eea;
  color: white;
  border-color: #667eea;
}

.action-btn.primary:hover {
  background: #5a6fd8;
  transform: translateY(-2px);
  box-shadow: 0 8px 20px rgba(102, 126, 234, 0.3);
}

.action-btn.secondary {
  border-color: #e9ecef;
  color: #6c757d;
  cursor: not-allowed;
  opacity: 0.7;
}

.btn-icon {
  font-size: 1.25rem;
  flex-shrink: 0;
}

.btn-text {
  flex-grow: 1;
}

.btn-arrow {
  font-size: 1.25rem;
  opacity: 0.7;
}

.btn-badge {
  background: #ffc107;
  color: #212529;
  padding: 0.25rem 0.5rem;
  border-radius: 4px;
  font-size: 0.75rem;
  font-weight: 600;
}

/* Activity Card */
.activity-icon {
  font-size: 1.5rem;
}

.activity-item {
  display: flex;
  align-items: center;
  gap: 1rem;
  margin-bottom: 1rem;
}

.activity-item:last-child {
  margin-bottom: 0;
}

.activity-dot {
  width: 8px;
  height: 8px;
  background: #28a745;
  border-radius: 50%;
  flex-shrink: 0;
}

.activity-dot.inactive {
  background: #ccc;
}

.activity-text {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.activity-action {
  font-weight: 500;
  color: #333;
}

.activity-time {
  font-size: 0.875rem;
  color: #666;
}

/* Responsive Design */
@media (max-width: 768px) {
  .page {
    padding: 0.5rem;
  }

  .header-content {
    padding: 1rem 1.5rem;
  }

  .logo {
    font-size: 1.25rem;
  }

  .dashboard {
    padding: 1.5rem;
  }

  .welcome-title {
    font-size: 2rem;
  }

  .dashboard-grid {
    grid-template-columns: 1fr;
    gap: 1.5rem;
  }

  .card-header {
    padding: 1rem 1.5rem;
  }

  .card-content {
    padding: 1.5rem;
  }

  .action-btn {
    padding: 0.875rem 1.25rem;
  }
}

@media (max-width: 480px) {
  .header-content {
    padding: 1rem;
  }

  .dashboard {
    padding: 1rem;
  }

  .welcome-title {
    font-size: 1.75rem;
  }

  .card-header {
    padding: 1rem;
  }

  .card-content {
    padding: 1rem;
  }
}
</style>
