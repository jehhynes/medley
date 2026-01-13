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
                          @open-fragment="$emit('open-fragment', $event)" />
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
                    @open-fragment="$emit('open-fragment', $event)" />
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
                <option value="Chat">Chat</option>
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

<script>
import ToolCallItem from './ToolCallItem.vue';

export default {
  name: 'ChatPanel',
  components: {
    ToolCallItem
  },
  props: {
    articleId: {
      type: String,
      default: null
    },
    connection: {
      type: Object,
      default: null
    }
  },
  emits: ['open-plan', 'open-fragment'],
  data() {
    return {
      conversationId: null,
      messages: [],
      newMessage: '',
      isAiTurn: false,
      error: null,
      isLoading: false,

      expandedMessages: {}, // Track expanded state of intermediate/tool messages
      mode: 'Chat' // Default mode
    };
  },
  computed: {
    isConnected() {
      return this.connection && this.connection.state === signalR.HubConnectionState.Connected;
    },
    hasMessages() {
      return this.messages.length > 0;
    },
    canSendMessage() {
      return this.articleId &&
        this.newMessage.trim() !== '' &&
        !this.isAiTurn;
    }
  },
  watch: {
    articleId: {
      immediate: true,
      async handler(newId, oldId) {
        if (newId !== oldId) {
          this.reset();

          if (newId) {
            this.loadActiveConversation();
          }
        }
      }
    },
    connection: {
      immediate: true,
      handler(newConn, oldConn) {
        if (oldConn) {
          this.removeEventListeners(oldConn);
        }
        if (newConn) {
          this.setupEventListeners(newConn);
        }
      }
    },
    newMessage() {
      this.$nextTick(() => {
        this.adjustTextareaHeight();
      });
    }
  },
  beforeUnmount() {
    if (this.connection) {
      this.removeEventListeners(this.connection);
    }
  },
  methods: {
    setupEventListeners(conn) {
      conn.on('ChatTurnStarted', this.onTurnStarted);
      conn.on('ChatMessageReceived', this.onMessageReceived);
      conn.on('ChatMessageStreaming', this.onMessageStreaming);
      conn.on('ChatToolInvoked', this.onToolInvoked);
      conn.on('ChatToolCompleted', this.onToolCompleted);
      conn.on('ChatMessageComplete', this.onMessageComplete);
      conn.on('ChatTurnComplete', this.onTurnComplete);
      conn.on('ChatError', this.onChatError);
    },

    removeEventListeners(conn) {
      conn.off('ChatTurnStarted', this.onTurnStarted);
      conn.off('ChatMessageReceived', this.onMessageReceived);
      conn.off('ChatMessageStreaming', this.onMessageStreaming);
      conn.off('ChatToolInvoked', this.onToolInvoked);
      conn.off('ChatToolCompleted', this.onToolCompleted);
      conn.off('ChatMessageComplete', this.onMessageComplete);
      conn.off('ChatTurnComplete', this.onTurnComplete);
      conn.off('ChatError', this.onChatError);
    },

    async loadActiveConversation() {
      if (!this.articleId) return;

      this.isLoading = true;
      this.error = null;

      try {
        const response = await fetch(`/api/articles/${this.articleId}/assistant/conversation`);

        if (response.status === 204) {
          // No active conversation - that's ok
          this.conversationId = null;
          this.messages = [];
        } else if (response.ok) {
          const conversation = await response.json();
          this.conversationId = conversation.id;
          this.mode = conversation.mode || 'Chat';
          await this.loadMessages();
        } else if (response.status === 404) {
          // No active conversation - that's ok (backwards compatibility)
          this.conversationId = null;
          this.messages = [];
        } else {
          throw new Error('Failed to load conversation');
        }
      } catch (err) {
        console.error('Error loading conversation:', err);
        this.error = 'Failed to load conversation';
      } finally {
        this.isLoading = false;
        // Scroll to bottom after loading is complete and DOM is updated
        // Use double $nextTick to ensure v-if directives have fully rendered
        this.$nextTick(() => {
          this.$nextTick(() => {
            this.scrollToBottom();
          });
        });
      }
    },

    async loadMessages() {
      if (!this.conversationId) return;

      try {
        const response = await fetch(
          `/api/articles/${this.articleId}/assistant/conversations/${this.conversationId}/messages`
        );

        if (response.ok) {
          this.messages = await response.json();

          // Check if the last message is from a user (AI is likely processing)
          if (this.messages.length > 0) {
            const lastMessage = this.messages[this.messages.length - 1];
            if (lastMessage.role === 'user') {
              this.isAiTurn = true;
            }
          }
        } else {
          throw new Error('Failed to load messages');
        }
      } catch (err) {
        console.error('Error loading messages:', err);
        this.error = 'Failed to load messages';
      }
    },

    async sendMessage() {
      if (!this.canSendMessage) return;

      const messageText = this.newMessage.trim();
      this.newMessage = '';
      this.error = null;

      try {
        // Create conversation if needed
        if (!this.conversationId) {
          const createResponse = await fetch(
            `/api/articles/${this.articleId}/assistant/conversation?mode=${this.mode}`,
            {
              method: 'POST',
              headers: { 'Content-Type': 'application/json' }
            }
          );

          if (!createResponse.ok) {
            throw new Error('Failed to create conversation');
          }

          const conversation = await createResponse.json();
          this.conversationId = conversation.id;
        }

        // Send message to API (message will appear via SignalR broadcast)
        const response = await fetch(
          `/api/articles/${this.articleId}/assistant/conversations/${this.conversationId}/messages`,
          {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
              message: messageText,
              mode: this.mode
            })
          }
        );

        if (!response.ok) {
          throw new Error('Failed to send message');
        }

        // Set AI thinking state
        this.isAiTurn = true;
        this.$nextTick(() => this.scrollToBottom());

      } catch (err) {
        console.error('Error sending message:', err);
        this.error = 'Failed to send message';
      }
    },

    onTurnStarted(data) {
      if (data.conversationId !== this.conversationId) {
        return;
      }

      this.isAiTurn = true;
      this.$nextTick(() => this.scrollToBottom());
    },

    onMessageReceived(data) {
      // Handle user messages broadcast via SignalR
      if (data.conversationId !== this.conversationId) {
        return;
      }
      
      // Add user message to list
      this.messages.push({
        id: data.id,
        role: data.role,
        text: data.text,
        userName: data.userName,
        createdAt: data.createdAt
      });

      this.$nextTick(() => this.scrollToBottom());
    },

    onMessageStreaming(data) {
      // Handle streaming text updates
      if (data.conversationId !== this.conversationId) {
        return;
      }

      // Use messageId if provided, otherwise fall back to temporary ID
      const messageId = data.messageId || 'streaming-temp';

      // Find or create the streaming message by ID
      let streamingMsg = this.messages.find(m => m.id === messageId && m.isStreaming);
      if (!streamingMsg) {
        // Create a new streaming message placeholder
        streamingMsg = {
          id: messageId,
          role: 'assistant',
          text: '',
          isStreaming: true,
          toolCalls: [],
          userName: null,
          createdAt: new Date().toISOString()
        };
        this.messages.push(streamingMsg);
      }

      // Append the streamed text
      streamingMsg.text += data.text;
      this.$nextTick(() => this.scrollToBottom());
    },

    onToolInvoked(data) {
      // Handle tool invocation notifications
      if (data.conversationId !== this.conversationId) {
        return;
      }

      console.log(`AI Agent invoked tool: ${data.toolName}`);

      // Use messageId if provided to associate tool with message
      const messageId = data.messageId || 'streaming-temp';

      // Find the streaming message and add tool invocation
      let streamingMsg = this.messages.find(m => m.id === messageId && m.isStreaming);
      if (!streamingMsg) {
        // Create a new streaming message if it doesn't exist yet
        streamingMsg = {
          id: messageId,
          role: 'assistant',
          text: '',
          isStreaming: true,
          toolCalls: [],
          userName: null,
          createdAt: new Date().toISOString()
        };
        this.messages.push(streamingMsg);
      }

      // Add tool call to the message (pending state)
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

      this.$nextTick(() => this.scrollToBottom());
    },

    onToolCompleted(data) {
      // Handle tool completion notifications
      if (data.conversationId !== this.conversationId) {
        return;
      }

      console.log(`AI Agent completed tool: ${data.toolName}`);

      // Loop backwards through messages to find the matching tool call by callId
      // (it will almost always be in the most recent message)
      for (let i = this.messages.length - 1; i >= 0; i--) {
        const msg = this.messages[i];
        if (msg.toolCalls && msg.toolCalls.length > 0) {
          const toolCall = msg.toolCalls.find(t => t.callId === data.toolCallId);
          if (toolCall) {
            toolCall.completed = true;
            toolCall.isError = data.isError || false;
            // Store the result with IDs if available
            if (data.result && data.result.ids) {
              toolCall.result = {
                ids: data.result.ids
              };
            }
            break; // Found and updated, stop searching
          }
        }
      }

      this.$nextTick(() => this.scrollToBottom());
    },

    onMessageComplete(data) {
      // Verify this is for the current conversation
      if (data.conversationId !== this.conversationId) {
        return;
      }

      // Find the streaming message by ID if it exists
      const streamingIdx = this.messages.findIndex(m =>
        m.isStreaming && (m.id === data.id || m.id === 'streaming-temp')
      );

      if (streamingIdx >= 0) {
        // Update the existing streaming message to be the final version
        const streamingMsg = this.messages[streamingIdx];
        this.messages.splice(streamingIdx, 1, {
          id: data.id,
          role: data.role,
          text: data.text,
          userName: data.userName,
          createdAt: data.createdAt,
          toolCalls: streamingMsg.toolCalls || [],
          isStreaming: false
        });
      } else {
        // No streaming message found, add the complete message
        this.messages.push({
          id: data.id,
          role: data.role,
          text: data.text,
          userName: data.userName,
          createdAt: data.createdAt,
          toolCalls: [],
          isStreaming: false
        });
      }

      this.$nextTick(() => this.scrollToBottom());
    },

    onTurnComplete(data) {
      // Verify this is for the current conversation
      if (data.conversationId !== this.conversationId) {
        return;
      }

      console.log('Turn complete');
      this.isAiTurn = false;
    },

    onChatError(data) {
      if (data.conversationId !== this.conversationId) {
        return;
      }

      console.error('Chat error:', data);
      this.error = data.error || 'An error occurred';
      this.isAiTurn = false;
    },

    scrollToBottom() {
      const container = this.$refs.messagesContainer;
      if (container) {
        container.scrollTop = container.scrollHeight;
      }
    },

    reset() {
      this.conversationId = null;
      this.messages = [];
      this.newMessage = '';
      this.isAiTurn = false;
      this.error = null;
      this.expandedMessages = {};
      this.mode = 'Chat';
    },

    toggleMessageExpansion(messageId) {
      this.expandedMessages[messageId] = !this.expandedMessages[messageId];
    },

    formatDate(dateString) {
      const date = new Date(dateString);
      const now = new Date();
      const diffMs = now - date;
      const diffMins = Math.floor(diffMs / 60000);

      if (diffMins < 1) return 'Just now';
      if (diffMins < 60) return `${diffMins}m ago`;

      const diffHours = Math.floor(diffMins / 60);
      if (diffHours < 24) return `${diffHours}h ago`;

      const diffDays = Math.floor(diffHours / 24);
      if (diffDays < 7) return `${diffDays}d ago`;

      return date.toLocaleDateString();
    },

    renderMarkdown(text) {
      if (!text) return '';
      // Use marked library to render markdown
      if (typeof marked !== 'undefined') {
        return marked.parse(text);
      }
      // Fallback to plain text if marked is not available
      return text.replace(/\n/g, '<br>');
    },

    openPlan(planId) {
      if (!planId) return;
      this.$emit('open-plan', planId);
    },

    async createPlan() {
      if (!this.articleId || this.isAiTurn) return;

      // Switch to Plan mode
      this.mode = 'Plan';

      // Set the command message
      this.newMessage = 'Create a plan for improving this article';

      // Send it using the standard chat flow
      await this.sendMessage();
    },

    adjustTextareaHeight() {
      const textarea = this.$refs.chatInput;
      if (textarea) {
        textarea.style.height = 'auto';
        textarea.style.height = Math.min(textarea.scrollHeight, 200) + 'px';
      }
    }
  }
};
</script>
