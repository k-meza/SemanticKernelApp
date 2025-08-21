<!-- src/views/ChatView.vue -->
<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue';
import { useRouter } from 'vue-router';
import { getCurrentUser, signOut } from 'aws-amplify/auth';

// SignalR will be added here
// import * as signalR from '@microsoft/signalr';

const router = useRouter();
const username = ref<string | null>(null);
const connectionStatus = ref<'disconnected' | 'connecting' | 'connected'>('disconnected');
const messages = ref<Array<{ user: string; message: string; timestamp: Date }>>([]);
const newMessage = ref('');
const error = ref<string | null>(null);

// Placeholder for SignalR connection
// let connection: signalR.HubConnection | null = null;

onMounted(async () => {
  try {
    const user = await getCurrentUser();
    username.value = user.username ?? 'Anonymous';

    // Initialize SignalR connection (placeholder)
    await initializeSignalR();
  } catch {
    error.value = 'Failed to authenticate user';
    router.push('/login');
  }
});

onUnmounted(() => {
  disconnectSignalR();
});

async function initializeSignalR() {
  try {
    connectionStatus.value = 'connecting';

    // TODO: Replace with actual SignalR implementation
    // connection = new signalR.HubConnectionBuilder()
    //   .withUrl('https://your-backend-url/chathub')
    //   .withAutomaticReconnect()
    //   .build();

    // connection.on('ReceiveMessage', (user: string, message: string) => {
    //   messages.value.push({
    //     user,
    //     message,
    //     timestamp: new Date()
    //   });
    // });

    // await connection.start();

    // Simulate connection for now
    setTimeout(() => {
      connectionStatus.value = 'connected';
      // Add a welcome message
      messages.value.push({
        user: 'System',
        message: 'Welcome to the chat! SignalR connection will be implemented here.',
        timestamp: new Date()
      });
    }, 1000);

  } catch (err) {
    connectionStatus.value = 'disconnected';
    error.value = 'Failed to connect to chat server';
    console.error('SignalR connection failed:', err);
  }
}

function disconnectSignalR() {
  // if (connection) {
  //   connection.stop();
  //   connection = null;
  // }
  connectionStatus.value = 'disconnected';
}

async function sendMessage() {
  if (!newMessage.value.trim() || connectionStatus.value !== 'connected') return;

  try {
    // TODO: Send message via SignalR
    // await connection?.invoke('SendMessage', username.value, newMessage.value);

    // Simulate sending message for now
    messages.value.push({
      user: username.value || 'You',
      message: newMessage.value,
      timestamp: new Date()
    });

    newMessage.value = '';
  } catch (err) {
    error.value = 'Failed to send message';
    console.error('Failed to send message:', err);
  }
}

async function handleSignOut() {
  try {
    await signOut();
    await router.push('/login');
  } catch {
    error.value = 'Failed to sign out';
  }
}

function goToHome() {
  router.push('/');
}
</script>

<template>
  <div class="chat-container">
    <div class="chat-header">
      <div class="header-left">
        <h1>Chat Room</h1>
        <div class="connection-status" :class="connectionStatus">
          <div class="status-indicator"></div>
          <span>{{ connectionStatus === 'connected' ? 'Connected' : connectionStatus === 'connecting' ? 'Connecting...' : 'Disconnected' }}</span>
        </div>
      </div>
      <div class="header-actions">
        <button @click="goToHome" class="btn btn-secondary">
          Home
        </button>
        <button @click="handleSignOut" class="btn btn-outline">
          Sign Out
        </button>
      </div>
    </div>

    <div v-if="error" class="error-banner">
      {{ error }}
      <button @click="error = null" class="close-error">×</button>
    </div>

    <div class="chat-content">
      <div class="messages-container">
        <div v-if="messages.length === 0" class="no-messages">
          <p>No messages yet. Start the conversation!</p>
        </div>
        <div
          v-for="(msg, index) in messages"
          :key="index"
          class="message"
          :class="{ 'own-message': msg.user === username || msg.user === 'You' }"
        >
          <div class="message-header">
            <span class="message-user">{{ msg.user }}</span>
            <span class="message-time">
              {{ msg.timestamp.toLocaleTimeString() }}
            </span>
          </div>
          <div class="message-content">{{ msg.message }}</div>
        </div>
      </div>

      <div class="message-input-container">
        <div class="message-input">
          <input
            v-model="newMessage"
            @keyup.enter="sendMessage"
            :disabled="connectionStatus !== 'connected'"
            type="text"
            placeholder="Type your message..."
            class="message-field"
          />
          <button
            @click="sendMessage"
            :disabled="!newMessage.trim() || connectionStatus !== 'connected'"
            class="send-button"
          >
            Send
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.chat-container {
  height: 100vh;
  display: flex;
  flex-direction: column;
  background: #f5f5f5;
}

.chat-header {
  background: white;
  padding: 1rem 2rem;
  border-bottom: 1px solid #e0e0e0;
  display: flex;
  justify-content: space-between;
  align-items: center;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.header-left {
  display: flex;
  align-items: center;
  gap: 2rem;
}

.header-left h1 {
  margin: 0;
  font-size: 1.5rem;
  color: #333;
}

.connection-status {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.9rem;
}

.status-indicator {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: #ccc;
}

.connection-status.connected .status-indicator {
  background: #28a745;
}

.connection-status.connecting .status-indicator {
  background: #ffc107;
  animation: pulse 1.5s infinite;
}

.connection-status.disconnected .status-indicator {
  background: #dc3545;
}

@keyframes pulse {
  0%, 100% { opacity: 1; }
  50% { opacity: 0.5; }
}

.header-actions {
  display: flex;
  gap: 1rem;
}

.error-banner {
  background: #f8d7da;
  color: #721c24;
  padding: 1rem 2rem;
  border-bottom: 1px solid #f5c6cb;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.close-error {
  background: none;
  border: none;
  font-size: 1.5rem;
  cursor: pointer;
  color: #721c24;
}

.chat-content {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.messages-container {
  flex: 1;
  overflow-y: auto;
  padding: 1rem 2rem;
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.no-messages {
  text-align: center;
  color: #666;
  font-style: italic;
  margin-top: 2rem;
}

.message {
  background: white;
  border-radius: 12px;
  padding: 1rem;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
  max-width: 70%;
  align-self: flex-start;
}

.message.own-message {
  align-self: flex-end;
  background: #667eea;
  color: white;
}

.message-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 0.5rem;
  font-size: 0.8rem;
}

.message-user {
  font-weight: 600;
}

.message-time {
  opacity: 0.7;
}

.message-content {
  line-height: 1.4;
}

.message-input-container {
  background: white;
  border-top: 1px solid #e0e0e0;
  padding: 1rem 2rem;
}

.message-input {
  display: flex;
  gap: 1rem;
  max-width: 100%;
}

.message-field {
  flex: 1;
  padding: 0.75rem 1rem;
  border: 2px solid #e0e0e0;
  border-radius: 8px;
  font-size: 1rem;
  outline: none;
  transition: border-color 0.3s;
}

.message-field:focus {
  border-color: #667eea;
}

.message-field:disabled {
  background: #f5f5f5;
  cursor: not-allowed;
}

.send-button {
  padding: 0.75rem 1.5rem;
  background: #667eea;
  color: white;
  border: none;
  border-radius: 8px;
  font-size: 1rem;
  cursor: pointer;
  transition: background 0.3s;
}

.send-button:hover:not(:disabled) {
  background: #5a6fd8;
}

.send-button:disabled {
  background: #ccc;
  cursor: not-allowed;
}

.btn {
  padding: 0.5rem 1rem;
  border-radius: 6px;
  font-size: 0.9rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.3s ease;
  border: 2px solid transparent;
  text-decoration: none;
  display: inline-block;
}

.btn-outline {
  background: transparent;
  border-color: #667eea;
  color: #667eea;
}

.btn-outline:hover {
  background: #667eea;
  color: white;
}

.btn-secondary {
  background: #6c757d;
  color: white;
  border-color: #6c757d;
}

.btn-secondary:hover {
  background: #5a6268;
}

@media (max-width: 768px) {
  .chat-header {
    padding: 1rem;
    flex-direction: column;
    gap: 1rem;
    align-items: stretch;
  }

  .header-left {
    justify-content: space-between;
  }

  .messages-container {
    padding: 1rem;
  }

  .message {
    max-width: 85%;
  }

  .message-input-container {
    padding: 1rem;
  }
}
</style>
