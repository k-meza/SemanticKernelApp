<!-- ChatView.vue -->
<template>
  <div class="chat-container">
    <!-- Header -->
    <div class="chat-header">
      <div class="header-left">
        <h1>AI Chat</h1>
        <div class="connection-status" :class="connectionStatusClass">
          <div class="status-indicator"></div>
          <span>{{ connectionStatusText }}</span>
        </div>
      </div>

      <div class="header-actions">
        <!-- Model Selector -->
        <div class="model-selector">
          <label for="model-select">Model:</label>
          <select
            id="model-select"
            v-model="selectedModel"
            :disabled="isStreaming || isModelLocked"
            class="model-dropdown"
            :class="{ 'locked': isModelLocked }"
          >
            <option v-for="model in availableModels" :key="model.value" :value="model.value">
              {{ model.label }}
            </option>
          </select>

          <!-- Lock/Unlock Button -->
          <button
            type="button"
            @click="toggleModelLock"
            :disabled="isStreaming"
            class="lock-button"
            :class="{ 'locked': isModelLocked }"
            :title="isModelLocked ? 'Unlock model selection' : 'Lock model selection'"
          >
            {{ isModelLocked ? '🔒' : '🔓' }}
          </button>
        </div>
      </div>
    </div>

    <!-- Chat Content -->
    <div class="chat-content">
      <!-- Model Lock Notice -->
      <div v-if="!isModelLocked && messages.length === 0" class="model-notice">
        <div class="notice-content">
          <span class="notice-icon">⚠️</span>
          <span>Please select a model and lock it to connect and start chatting</span>
        </div>
      </div>

      <!-- Connection Notice -->
      <div v-if="isModelLocked && isConnecting" class="connection-notice">
        <div class="notice-content">
          <span class="notice-icon">🔄</span>
          <span>Connecting to chat service...</span>
        </div>
      </div>

      <!-- Messages -->
      <div class="messages-container">
        <div v-if="messages.length === 0 && isModelLocked && !isConnecting" class="no-messages">
          Start a conversation with <strong>{{ getModelLabel(selectedModel) }}</strong>
        </div>

        <div
          v-for="(message, index) in messages"
          :key="index"
          class="message"
          :class="{ 'own-message': message.role === 'user' }"
        >
          <div class="message-header">
            <div class="message-user">
              {{ message.role === 'user' ? 'You' : getModelLabel(selectedModel) }}
            </div>
            <div class="message-time">
              {{ formatTime(new Date()) }}
            </div>
          </div>
          <div class="message-content">{{ message.content }}</div>
        </div>
      </div>

      <!-- Input Area -->
      <div class="message-input-container">
        <form @submit.prevent="onSend" class="message-input">
          <input
            v-model="userInput"
            type="text"
            class="message-field"
            :placeholder="inputPlaceholder"
            :disabled="isConnecting || isStreaming || !isModelLocked"
          />
          <button
            type="submit"
            class="send-button"
            :disabled="!canSend"
          >
            {{ isStreaming ? 'Sending...' : 'Send' }}
          </button>
          <button
            v-if="isStreaming"
            type="button"
            class="btn btn-secondary"
            @click="cancelStream"
          >
            Cancel
          </button>
        </form>
      </div>
    </div>
  </div>
</template>

<script lang="ts" setup>
import { onMounted, onBeforeUnmount, ref, computed, watch } from 'vue'
import {
  HubConnectionBuilder,
  LogLevel,
  HttpTransportType,
  HubConnection,
  type IStreamResult,
} from '@microsoft/signalr'

type ChatMessage = {
  role: 'user' | 'assistant'
  content: string
}

type ModelOption = {
  value: string
  label: string
}

type Props = {
  // Connection endpoint (if not provided, will use VITE_BACKEND_URL + '/chathub')
  hubUrl?: string

  // Generation controls
  temperature?: number
  maxTokens?: number

  // Optional system prompt
  systemPrompt?: string | null
}

const props = withDefaults(defineProps<Props>(), {
  temperature: 0.2,
  maxTokens: 800,
  systemPrompt: null,
})

// Available OpenAI models
const availableModels: ModelOption[] = [
  { value: 'gpt-4o-mini', label: 'GPT-4o Mini' },
  { value: 'gpt-4o', label: 'GPT-4o' },
  { value: 'gpt-4-turbo', label: 'GPT-4 Turbo' },
  { value: 'gpt-4', label: 'GPT-4' },
  { value: 'gpt-3.5-turbo', label: 'GPT-3.5 Turbo' },
]

// Reactive state
const selectedModel = ref('gpt-4o-mini')
const isModelLocked = ref(false)
const connection = ref<HubConnection | null>(null)
const isConnecting = ref(false) // Start as false, only connect when locked
const isStreaming = ref(false)
const userInput = ref('')
const messages = ref<ChatMessage[]>([])

// Compute effective hub URL using env if prop not provided
const backendUrl = (import.meta.env.VITE_API_BASE_URL as string) || 'http://localhost:5000'
const hubUrl = (props.hubUrl && props.hubUrl.trim()) ? props.hubUrl : `${backendUrl}/chat`

let currentSubscription: { dispose: () => void } | null = null

// Watch for model lock changes and manage connection accordingly
watch(isModelLocked, async (newLocked, oldLocked) => {
  if (newLocked && !oldLocked) {
    // Model was just locked - establish connection
    await initConnection()
  } else if (!newLocked && oldLocked) {
    // Model was unlocked - disconnect and clear messages
    await disconnectFromHub()
    // Clear chat history when unlocking
    if (connection.value) {
      try {
        await connection.value.invoke('ClearHistory')
      } catch (err) {
        console.warn('Failed to clear history on server:', err)
      }
    }
  }
})

// ... existing code ...
const connectionStatusClass = computed(() => {
  if (!isModelLocked.value) return 'disconnected'
  if (isConnecting.value) return 'connecting'
  return connection.value ? 'connected' : 'disconnected'
})

const connectionStatusText = computed(() => {
  if (!isModelLocked.value) return 'Model not locked'
  if (isConnecting.value) return 'Connecting...'
  return connection.value ? 'Connected' : 'Disconnected'
})

const inputPlaceholder = computed(() => {
  if (!isModelLocked.value) return 'Lock a model first to start chatting...'
  if (isConnecting.value) return 'Connecting to server...'
  if (isStreaming.value) return 'AI is responding...'
  if (!connection.value) return 'Connection failed, please try again...'
  return 'Type your message here...'
})

const canSend = computed(() => {
  return !!userInput.value.trim() &&
    !isConnecting.value &&
    !isStreaming.value &&
    !!connection.value &&
    isModelLocked.value
})

// Helper functions
function formatTime(date: Date): string {
  return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
}

function getModelLabel(modelValue: string): string {
  const model = availableModels.find(m => m.value === modelValue)
  return model?.label || modelValue
}

async function toggleModelLock() {
  if (isStreaming.value) return

  const wasLocked = isModelLocked.value

  // If unlocking and there are messages, ask for confirmation
  if (wasLocked && messages.value.length > 0) {
    const confirmUnlock = confirm(
      'Unlocking the model will disconnect and clear the current conversation. Are you sure?'
    )

    if (!confirmUnlock) {
      return
    }

    messages.value = []
  }

  isModelLocked.value = !wasLocked
}

async function initConnection() {
  if (!isModelLocked.value || connection.value) return

  isConnecting.value = true

  try {
    const conn = new HubConnectionBuilder()
      .withUrl(hubUrl, {
        transport: HttpTransportType.WebSockets,
      })
      // Custom retry delays in milliseconds
      .withAutomaticReconnect([0, 2000, 10000, 30000])
      .configureLogging(LogLevel.Information)
      .build()

    // Set up event handlers
    conn.onreconnecting(() => {
      console.log('Connection lost, attempting to reconnect...')
      isConnecting.value = true
    })

    conn.onreconnected(() => {
      console.log('Successfully reconnected')
      isConnecting.value = false
    })

    conn.onclose((error) => {
      console.log('Connection closed:', error)
      isConnecting.value = false
      connection.value = null
    })

    // Start the connection
    await conn.start()
    connection.value = conn
    isConnecting.value = false
    console.log('SignalR connection established')

  } catch (err) {
    console.error('Failed to connect to ChatHub:', err)
    isConnecting.value = false
    connection.value = null
  }
}

async function disconnectFromHub() {
  if (currentSubscription) {
    currentSubscription.dispose()
    currentSubscription = null
  }

  if (connection.value) {
    try {
      await connection.value.stop()
      console.log('SignalR connection closed')
    } catch (err) {
      console.error('Error closing connection:', err)
    } finally {
      connection.value = null
    }
  }

  isConnecting.value = false
}

function appendUserMessage(content: string) {
  messages.value.push({ role: 'user', content })
}

function appendAssistantMessageContainer(): number {
  messages.value.push({ role: 'assistant', content: '' })
  return messages.value.length - 1
}

function updateAssistantMessage(index: number, delta: string) {
  const msg = messages.value[index]
  if (!msg || msg.role !== 'assistant') return
  msg.content += delta
}

// Send and stream
async function onSend() {
  const text = userInput.value.trim()
  if (!text || !connection.value || isStreaming.value || !isModelLocked.value) return

  // 1) Add user message locally
  appendUserMessage(text)
  userInput.value = ''

  // 2) Prepare an assistant message container to be filled by streamed tokens
  const assistantIndex = appendAssistantMessageContainer()

  // 3) Start streaming - using the new simplified API
  isStreaming.value = true

  try {
    const stream: IStreamResult<string> = connection.value.stream(
      'StreamChat',
      selectedModel.value,    // modelId
      text,                   // userMessage
      props.temperature,      // temperature (optional)
      props.maxTokens,        // maxTokens (optional)
      props.systemPrompt      // systemPrompt (optional)
    )

    // 4) Subscribe to the stream
    currentSubscription = stream.subscribe({
      next: (token: string) => {
        // Append token as it arrives
        if (token) updateAssistantMessage(assistantIndex, token)
      },
      complete: () => {
        isStreaming.value = false
        currentSubscription = null
        // If this was the first message and we didn't have a session ID,
        // the server should have created one. You might need to modify your
        // backend to return the session ID somehow, or implement a separate
        // method to get it.
      },
      error: (err: Error) => {
        console.error('Stream error:', err)
        isStreaming.value = false
        currentSubscription = null
      },
    })
  } catch (err) {
    console.error('Failed to start stream:', err)
    isStreaming.value = false
    currentSubscription = null
  }
}

function cancelStream() {
  if (currentSubscription) {
    currentSubscription.dispose()
    currentSubscription = null
  }
  isStreaming.value = false
}

// Component lifecycle - no automatic connection
onMounted(() => {
  // Don't automatically connect - wait for model lock
  console.log('ChatView mounted - waiting for model lock before connecting')
})

onBeforeUnmount(async () => {
  try {
    await disconnectFromHub()
  } catch {
    // noop
  }
})
</script>

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
  align-items: center;
}

.model-selector {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.model-selector label {
  font-size: 0.9rem;
  color: #666;
  font-weight: 500;
}

.model-dropdown {
  padding: 0.5rem 0.75rem;
  border: 2px solid #e0e0e0;
  border-radius: 6px;
  font-size: 0.9rem;
  background: white;
  cursor: pointer;
  transition: all 0.3s;
  min-width: 140px;
}

.model-dropdown:focus {
  outline: none;
  border-color: #667eea;
}

.model-dropdown:disabled {
  background: #f5f5f5;
  cursor: not-allowed;
  opacity: 0.7;
}

.model-dropdown.locked {
  border-color: #28a745;
  background: #f8f9fa;
  font-weight: 600;
}

.lock-button {
  padding: 0.5rem 0.75rem;
  border: 2px solid #e0e0e0;
  border-radius: 6px;
  background: white;
  cursor: pointer;
  font-size: 1rem;
  transition: all 0.3s;
  display: flex;
  align-items: center;
  justify-content: center;
  min-width: 44px;
}

.lock-button:hover:not(:disabled) {
  border-color: #667eea;
  background: #f8f9ff;
}

.lock-button:disabled {
  cursor: not-allowed;
  opacity: 0.5;
}

.lock-button.locked {
  border-color: #28a745;
  background: #d4edda;
}

.lock-button.locked:hover:not(:disabled) {
  background: #c3e6cb;
}

.chat-content {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.model-notice {
  background: #fff3cd;
  border: 1px solid #ffeaa7;
  border-left: 4px solid #fdcb6e;
  padding: 1rem 2rem;
  margin: 0;
}

.connection-notice {
  background: #d1ecf1;
  border: 1px solid #bee5eb;
  border-left: 4px solid #17a2b8;
  padding: 1rem 2rem;
  margin: 0;
}

.notice-content {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.9rem;
}

.model-notice .notice-content {
  color: #856404;
}

.connection-notice .notice-content {
  color: #0c5460;
}

.notice-icon {
  font-size: 1.1rem;
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
  white-space: pre-wrap;
  word-break: break-word;
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
  white-space: nowrap;
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
  white-space: nowrap;
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

  .header-actions {
    justify-content: center;
  }

  .model-notice,
  .connection-notice {
    padding: 1rem;
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

  .message-input {
    flex-wrap: wrap;
  }

  .message-field {
    min-width: 0;
  }
}
</style>
