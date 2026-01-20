<template>
  <div class="chat-panel">
    <div v-if="isLoading" class="loading-state">
      <div class="spinner-border spinner-border-sm" role="status">
        <span class="visually-hidden">Loading...</span>
      </div>
    </div>

    <div v-else-if="!articleId" class="empty-state" v-cloak>
      <i class="bi bi-chat-dots empty-state-icon"></i>
      <p class="empty-state-text">Select an article to start a conversation</p>
    </div>

    <template v-else>

      <div class="chat-messages" ref="messagesContainer">
        <div v-if="!hasMessages && !isAiTurn" class="empty-state" v-cloak>
          <i class="bi bi-chat-dots empty-state-icon"></i>
          <p class="empty-state-text">Start a conversation with the AI assistant</p>
          <p class="empty-state-hint">Ask questions or request improvements to your article</p>
          <button 
            @click="createPlan"
            :disabled="isAiTurn"
            class="btn btn-primary mt-3">
            <i class="far fa-magnifying-glass-play"></i>
            Create a Plan
          </button>
        </div>

        <template v-for="(msg, index) in messages" :key="msg.id">
          <!-- User message -->
          <div v-if="msg.role === 'user'" 
               class="chat-message chat-message-user">
            <div class="chat-message-header">
              <span class="chat-message-author">{{ msg.userName }}</span>
              <span class="chat-message-time">{{ formatDate(msg.createdAt) }}</span>
            </div>
            <div class="chat-message-body">{{ msg.text }}</div>
          </div>

          <!-- Assistant message -->
          <div v-else 
               class="chat-message chat-message-assistant">

            <div class="chat-message-body" 
                 :class="{ 'text-muted': msg.toolCalls && msg.toolCalls.length > 0 && !msg.isStreaming && index < messages.length - 1 }">
              
              <!-- Tool Invocation / Intermediate Step (Collapsed/Muted View) -->
              <!-- Show this style if: Has tools AND is NOT streaming AND is NOT the last message -->
              <template v-if="msg.toolCalls && msg.toolCalls.length > 0 && !msg.isStreaming && index < messages.length - 1">
                <div class="d-flex align-items-start ">
                  <button class="btn btn-link btn-sm p-0 text-muted" 
                          style="text-decoration: none; min-width: 1.5rem;"
                          @click="toggleMessageExpansion(msg.id)">
                    <i class="bi" :class="expandedMessages[msg.id] ? 'bi-chevron-down' : 'bi-chevron-right'"></i>
                  </button>
                  
                  <div class="flex-grow-1" style="min-width: 0;">
                    <!-- Collapsible content -->
                    <div v-if="expandedMessages[msg.id]">
                      <div class="markdown-container"
                           v-html="renderMarkdown(msg.text) || 'Thinking...'"></div>
                      
                      <div class="chat-message-tools mt-2">
                        <tool-call-item
                          v-for="(tool, idx) in msg.toolCalls"
                          :key="idx"
                          :tool="tool"
                          @open-plan="openPlan"
                          @open-fragment="$emit('open-fragment', $event)"
                          @open-version="$emit('open-version', $event)" />
                      </div>
                    </div>
                    
                    <!-- Collapsed summary -->
                    <div v-else class="chat-message-preview" 
                         @click="toggleMessageExpansion(msg.id)"
                         :title="msg.text">
                      {{ msg.text || 'Thinking...' }}
                    </div>
                  </div>
                </div>
              </template>

              <!-- Active/Final View (Full Content + Tools if any) -->
              <template v-else>
                <div class="markdown-container"
                     v-html="renderMarkdown(msg.text)"></div>
                
                <!-- Always show tools for active or non-collapsed messages -->
                <div v-if="msg.toolCalls && msg.toolCalls.length > 0" 
                     class="chat-message-tools mt-2">
                  <tool-call-item
                    v-for="(tool, idx) in msg.toolCalls"
                    :key="idx"
                    :tool="tool"
                    @open-plan="openPlan"
                    @open-fragment="$emit('open-fragment', $event)"
                    @open-version="$emit('open-version', $event)" />
                </div>
              </template>
            </div>
          </div>
        </template>

        <div v-if="isAiTurn" class="chat-message chat-message-assistant thinking">
          <div class="chat-message-body">
            <span class="typing-indicator">
              <span></span>
              <span></span>
              <span></span>
            </span>
          </div>
        </div>
      </div>

      <div class="chat-input-container">
        <div class="chat-input-box">
          <textarea 
            ref="chatInput"
            v-model="newMessage"
            @input="adjustTextareaHeight"
            @keydown.enter.exact.prevent="sendMessage"
            :disabled="!articleId || isAiTurn"
            class="form-control chat-input"
            placeholder="Ask the AI assistant..."
            rows="1"></textarea>
          
          <div class="chat-input-footer">
            <div class="chat-mode-selector">
              <select v-model="mode" 
                      :disabled="isAiTurn"
                      class="form-select form-select-sm mode-select">
                <option value="Agent">Agent</option>
                <option value="Plan">Plan</option>
              </select>
            </div>
            
            <button 
              @click="sendMessage"
              :disabled="!canSendMessage"
              class="btn btn-primary chat-send-btn">
              <i class="bi bi-arrow-right-short"></i>
            </button>
          </div>
        </div>
      </div>
    </template>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, nextTick, onBeforeUnmount } from 'vue';
import * as signalR from '@microsoft/signalr';
import ToolCallItem from './ToolCallItem.vue';
import { formatRelativeTime } from '@/utils/helpers';
import type { HubConnection } from '@microsoft/signalr';
import { ConversationMode } from '@/types/api-client';
import type {
  ChatTurnStartedPayload,
  ChatTurnCompletePayload,
  ChatMessageStreamingPayload,
  ChatToolInvokedPayload,
  ChatToolCompletedPayload,
  ChatMessageCompletePayload,
  ChatErrorPayload
} from '@/types/signalr/article-hub';

type ChatMessageRole = 'user' | 'assistant';

interface ToolCall {
  name: string;
  callId: string;
  display: string | null;
  completed: boolean;
  isError?: boolean;
  result?: {
    ids: string[];
  };
  timestamp: string;
}

interface ChatMessage {
  id: string;
  role: ChatMessageRole;
  text: string;
  userName?: string | null;
  createdAt: string;
  toolCalls?: ToolCall[];
  isStreaming?: boolean;
}

interface Conversation {
  id: string;
  mode: ConversationMode;
  implementingPlanId?: string | null;
  implementingPlanVersion?: number | null;
}

interface Props {
  articleId: string | null;
  connection: HubConnection | null;
}

const props = withDefaults(defineProps<Props>(), {
  articleId: null,
  connection: null
});

interface Emits {
  (e: 'open-plan', planId: string): void;
  (e: 'open-fragment', fragmentId: string): void;
  (e: 'open-version', versionId: string): void;
}

const emit = defineEmits<Emits>();

const conversationId = ref<string | null>(null);
const messages = ref<ChatMessage[]>([]);
const newMessage = ref<string>('');
const isAiTurn = ref<boolean>(false);
const error = ref<string | null>(null);
const isLoading = ref<boolean>(false);
const expandedMessages = ref<Record<string, boolean>>({});
const mode = ref<ConversationMode>(ConversationMode.Agent);
const implementingPlanId = ref<string | null>(null);
const implementingPlanVersion = ref<number | null>(null);

const messagesContainer = ref<HTMLElement | null>(null);
const chatInput = ref<HTMLTextAreaElement | null>(null);

const isConnected = computed<boolean>(() => {
  return !!(props.connection && props.connection.state === signalR.HubConnectionState.Connected);
});

const hasMessages = computed<boolean>(() => {
  return messages.value.length > 0;
});

const canSendMessage = computed<boolean>(() => {
  return !!(props.articleId &&
    newMessage.value.trim() !== '' &&
    !isAiTurn.value);
});

watch(() => props.articleId, async (newId, oldId) => {
  if (newId !== oldId) {
    reset();

    if (newId) {
      await loadConversation();
    }
  }
}, { immediate: true });

watch(() => props.connection, (newConn, oldConn) => {
  if (oldConn) {
    removeEventListeners(oldConn);
  }
  if (newConn) {
    setupEventListeners(newConn);
  }
}, { immediate: true });

watch(newMessage, () => {
  nextTick(() => {
    adjustTextareaHeight();
  });
});

onBeforeUnmount(() => {
  if (props.connection) {
    removeEventListeners(props.connection);
  }
});

function setupEventListeners(conn: HubConnection) {
  conn.on('ChatTurnStarted', onTurnStarted);
  conn.on('ChatMessageReceived', onMessageReceived);
  conn.on('ChatMessageStreaming', onMessageStreaming);
  conn.on('ChatToolInvoked', onToolInvoked);
  conn.on('ChatToolCompleted', onToolCompleted);
  conn.on('ChatMessageComplete', onMessageComplete);
  conn.on('ChatTurnComplete', onTurnComplete);
  conn.on('ChatError', onChatError);
}

function removeEventListeners(conn: HubConnection) {
  conn.off('ChatTurnStarted', onTurnStarted);
  conn.off('ChatMessageReceived', onMessageReceived);
  conn.off('ChatMessageStreaming', onMessageStreaming);
  conn.off('ChatToolInvoked', onToolInvoked);
  conn.off('ChatToolCompleted', onToolCompleted);
  conn.off('ChatMessageComplete', onMessageComplete);
  conn.off('ChatTurnComplete', onTurnComplete);
  conn.off('ChatError', onChatError);
}

async function loadConversation(conversationIdParam: string | null = null): Promise<void> {
  if (!props.articleId) return;

  isLoading.value = true;
  error.value = null;

  try {
    const url = conversationIdParam
      ? `/api/articles/${props.articleId}/assistant/conversation/${conversationIdParam}`
      : `/api/articles/${props.articleId}/assistant/conversation`;

    const response = await fetch(url);

    if (response.status === 204) {
      if (!conversationIdParam) {
        conversationId.value = null;
        messages.value = [];
        return;
      }
      throw new Error('Conversation not found');
    }

    if (response.status === 404) {
      if (conversationIdParam) {
        error.value = 'Conversation not found';
        return;
      }
      conversationId.value = null;
      messages.value = [];
      return;
    }

    if (!response.ok) {
      throw new Error('Failed to load conversation');
    }

    const conversation: Conversation = await response.json();
    conversationId.value = conversation.id;
    mode.value = conversation.mode || ConversationMode.Agent;
    implementingPlanId.value = conversation.implementingPlanId || null;
    implementingPlanVersion.value = conversation.implementingPlanVersion || null;

    await loadMessages();
  } catch (err) {
    console.error('Error loading conversation:', err);
    error.value = (err as Error).message || 'Failed to load conversation';
  } finally {
    isLoading.value = false;
    nextTick(() => {
      nextTick(() => {
        scrollToBottom();
      });
    });
  }
}

async function loadMessages(): Promise<void> {
  if (!conversationId.value) return;

  try {
    const response = await fetch(
      `/api/articles/${props.articleId}/assistant/conversations/${conversationId.value}/messages`
    );

    if (response.ok) {
      messages.value = await response.json();

      if (messages.value.length > 0) {
        const lastMessage = messages.value[messages.value.length - 1];
        if (lastMessage && lastMessage.role === 'user') {
          isAiTurn.value = true;
        }
      }
    } else {
      throw new Error('Failed to load messages');
    }
  } catch (err) {
    console.error('Error loading messages:', err);
    error.value = 'Failed to load messages';
  }
}

async function sendMessage(): Promise<void> {
  if (!canSendMessage.value) return;

  const messageText = newMessage.value.trim();
  newMessage.value = '';
  error.value = null;

  try {
    if (!conversationId.value) {
      const createResponse = await fetch(
        `/api/articles/${props.articleId}/assistant/conversation?mode=${mode.value}`,
        {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' }
        }
      );

      if (!createResponse.ok) {
        throw new Error('Failed to create conversation');
      }

      const conversation: Conversation = await createResponse.json();
      conversationId.value = conversation.id;
    }

    const response = await fetch(
      `/api/articles/${props.articleId}/assistant/conversations/${conversationId.value}/messages`,
      {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          message: messageText,
          mode: mode.value
        })
      }
    );

    if (!response.ok) {
      throw new Error('Failed to send message');
    }

    isAiTurn.value = true;
    nextTick(() => scrollToBottom());

  } catch (err) {
    console.error('Error sending message:', err);
    error.value = 'Failed to send message';
  }
}

function onTurnStarted(data: ChatTurnStartedPayload) {
  if (data.conversationId !== conversationId.value) {
    return;
  }

  isAiTurn.value = true;
  nextTick(() => scrollToBottom());
}

function onMessageReceived(data: any) {
  if (data.conversationId !== conversationId.value) {
    return;
  }
  
  messages.value.push({
    id: data.id,
    role: data.role,
    text: data.text,
    userName: data.userName,
    createdAt: data.createdAt
  });

  nextTick(() => scrollToBottom());
}

function onMessageStreaming(data: ChatMessageStreamingPayload) {
  if (data.conversationId !== conversationId.value) {
    return;
  }

  const messageId = data.messageId || 'streaming-temp';

  let streamingMsg = messages.value.find(m => m.id === messageId && m.isStreaming);
  if (!streamingMsg) {
    streamingMsg = {
      id: messageId,
      role: 'assistant',
      text: '',
      isStreaming: true,
      toolCalls: [],
      userName: null,
      createdAt: new Date().toISOString()
    };
    messages.value.push(streamingMsg);
  }

  streamingMsg.text += data.text || '';
  nextTick(() => scrollToBottom());
}

function onToolInvoked(data: ChatToolInvokedPayload) {
  if (data.conversationId !== conversationId.value) {
    return;
  }

  console.log(`AI Agent invoked tool: ${data.toolName}`);

  const messageId = (data as any).messageId || 'streaming-temp';

  let streamingMsg = messages.value.find(m => m.id === messageId && m.isStreaming);
  if (!streamingMsg) {
    streamingMsg = {
      id: messageId,
      role: 'assistant',
      text: '',
      isStreaming: true,
      toolCalls: [],
      userName: null,
      createdAt: new Date().toISOString()
    };
    messages.value.push(streamingMsg);
  }

  if (!streamingMsg.toolCalls) {
    streamingMsg.toolCalls = [];
  }

  streamingMsg.toolCalls.push({
    name: data.toolName,
    callId: data.toolCallId,
    display: data.toolDisplay,
    completed: false,
    timestamp: data.timestamp
  });

  nextTick(() => scrollToBottom());
}

function onToolCompleted(data: ChatToolCompletedPayload) {
  if (data.conversationId !== conversationId.value) {
    return;
  }

  console.log(`AI Agent completed tool: ${(data as any).toolName}`);

  for (let i = messages.value.length - 1; i >= 0; i--) {
    const msg = messages.value[i];
    if (msg && msg.toolCalls && msg.toolCalls.length > 0) {
      const toolCall = msg.toolCalls.find(t => t.callId === data.toolCallId);
      if (toolCall) {
        toolCall.completed = true;
        toolCall.isError = (data as any).isError || false;
        if ((data as any).result && (data as any).result.ids) {
          toolCall.result = {
            ids: (data as any).result.ids
          };
        }
        break;
      }
    }
  }

  nextTick(() => scrollToBottom());
}

function onMessageComplete(data: ChatMessageCompletePayload) {
  if (data.conversationId !== conversationId.value) {
    return;
  }

  const streamingIdx = messages.value.findIndex(m =>
    m.isStreaming && (m.id === data.id || m.id === 'streaming-temp')
  );

  if (streamingIdx >= 0) {
    const streamingMsg = messages.value[streamingIdx];
    if (streamingMsg) {
      messages.value.splice(streamingIdx, 1, {
        id: data.id,
        role: (data as any).role,
        text: (data as any).text || data.content,
        userName: (data as any).userName,
        createdAt: (data as any).createdAt || data.timestamp,
        toolCalls: streamingMsg.toolCalls || [],
        isStreaming: false
      });
    }
  } else {
    messages.value.push({
      id: data.id,
      role: (data as any).role || 'assistant',
      text: (data as any).text || data.content,
      userName: (data as any).userName,
      createdAt: (data as any).createdAt || data.timestamp,
      toolCalls: [],
      isStreaming: false
    });
  }

  nextTick(() => scrollToBottom());
}

function onTurnComplete(data: ChatTurnCompletePayload) {
  if (data.conversationId !== conversationId.value) {
    return;
  }

  console.log('Turn complete');
  isAiTurn.value = false;
}

function onChatError(data: ChatErrorPayload) {
  if (data.conversationId !== conversationId.value) {
    return;
  }

  console.error('Chat error:', data);
  error.value = data.message || 'An error occurred';
  isAiTurn.value = false;
}

function scrollToBottom(): void {
  const container = messagesContainer.value;
  if (container) {
    container.scrollTop = container.scrollHeight;
  }
}

function reset(): void {
  conversationId.value = null;
  messages.value = [];
  newMessage.value = '';
  isAiTurn.value = false;
  error.value = null;
  expandedMessages.value = {};
  mode.value = ConversationMode.Agent;
  implementingPlanId.value = null;
  implementingPlanVersion.value = null;
}

function toggleMessageExpansion(messageId: string): void {
  expandedMessages.value[messageId] = !expandedMessages.value[messageId];
}

function formatDate(dateString: string): string {
  return formatRelativeTime(dateString, { short: true });
}

function renderMarkdown(text: string): string {
  if (!text) return '';
  // Use marked library to render markdown
  if (typeof (window as any).marked !== 'undefined') {
    return (window as any).marked.parse(text);
  }
  // Fallback to plain text if marked is not available
  return text.replace(/\n/g, '<br>');
}

function openPlan(planId: string): void {
  if (!planId) return;
  emit('open-plan', planId);
}

async function createPlan(): Promise<void> {
  if (!props.articleId || isAiTurn.value) return;

  // Switch to Plan mode
  mode.value = ConversationMode.Plan;

  // Set the command message
  newMessage.value = 'Create a plan for improving this article';

  // Send it using the standard chat flow
  await sendMessage();
}

function adjustTextareaHeight(): void {
  const textarea = chatInput.value;
  if (textarea) {
    textarea.style.height = 'auto';
    textarea.style.height = Math.min(textarea.scrollHeight, 200) + 'px';
  }
}
</script>

