// Chat Panel Component - AI-powered article improvement
const ChatPanel = {
    name: 'ChatPanel',
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
    data() {
        return {
            conversationId: null,
            messages: [],
            newMessage: '',
            isAiThinking: false,
            error: null,
            isLoading: false,
            isCreatingPlan: false,
            turnExpansionState: {}, // Track which turns are expanded
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
                !this.isAiThinking;
        },
        groupedMessages() {
            // Group consecutive assistant messages into "turns"
            const grouped = [];
            let currentAssistantTurn = null;

            for (const message of this.messages) {
                if (message.role === 'user') {
                    // Close any current assistant turn
                    currentAssistantTurn = null;

                    // Add user message as standalone
                    grouped.push({
                        type: 'user',
                        ...message
                    });
                } else {
                    // Non-user message (assistant, tool)
                    if (currentAssistantTurn) {
                        // Add to existing turn
                        currentAssistantTurn.messages.push(message);
                        currentAssistantTurn.latestMessage = message;
                    } else {
                        // Start a new turn
                        const turnId = `turn-${message.id}`;
                        currentAssistantTurn = {
                            type: 'assistant-turn',
                            id: turnId,
                            messages: [message],
                            latestMessage: message,
                            expanded: this.turnExpansionState[turnId] || false
                        };
                        grouped.push(currentAssistantTurn);
                    }
                }
            }

            return grouped;
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
        }
    },
    beforeUnmount() {
        if (this.connection) {
            this.removeEventListeners(this.connection);
        }
    },
    methods: {
        setupEventListeners(conn) {
            conn.on('ChatMessageProcessing', this.onMessageProcessing);
            conn.on('ChatMessageReceived', this.onMessageReceived);
            conn.on('ChatMessageStreaming', this.onMessageStreaming);
            conn.on('ChatToolInvoked', this.onToolInvoked);
            conn.on('ChatToolCompleted', this.onToolCompleted);
            conn.on('ChatMessageComplete', this.onMessageComplete);
            conn.on('ChatError', this.onChatError);
        },

        removeEventListeners(conn) {
            conn.off('ChatMessageProcessing', this.onMessageProcessing);
            conn.off('ChatMessageReceived', this.onMessageReceived);
            conn.off('ChatMessageStreaming', this.onMessageStreaming);
            conn.off('ChatToolInvoked', this.onToolInvoked);
            conn.off('ChatToolCompleted', this.onToolCompleted);
            conn.off('ChatMessageComplete', this.onMessageComplete);
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
                            this.isAiThinking = true;
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
                this.isAiThinking = true;
                this.$nextTick(() => this.scrollToBottom());

            } catch (err) {
                console.error('Error sending message:', err);
                this.error = 'Failed to send message';
            }
        },

        onMessageProcessing(data) {
            this.isAiThinking = true;
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
                content: data.content,
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
                    content: '',
                    isStreaming: true,
                    toolCalls: [],
                    userName: 'Medley Assistant',
                    createdAt: new Date().toISOString()
                };
                this.messages.push(streamingMsg);
            }

            // Append the streamed content
            streamingMsg.content += data.content;
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
                    content: '',
                    isStreaming: true,
                    toolCalls: [],
                    userName: 'Medley Assistant',
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
                    content: data.content,
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
                    content: data.content,
                    userName: data.userName,
                    createdAt: data.createdAt,
                    toolCalls: [],
                    isStreaming: false
                });
            }

            this.isAiThinking = false;
            this.$nextTick(() => this.scrollToBottom());
        },

        onChatError(data) {
            console.error('Chat error:', data);
            this.error = data.error || 'An error occurred';
            this.isAiThinking = false;
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
            this.isAiThinking = false;
            this.error = null;
            this.turnExpansionState = {};
            this.mode = 'Chat';
        },

        toggleTurnExpansion(turnId) {
            this.turnExpansionState[turnId] = !this.turnExpansionState[turnId];
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

        renderMarkdown(content) {
            if (!content) return '';
            // Use marked library to render markdown
            if (typeof marked !== 'undefined') {
                return marked.parse(content);
            }
            // Fallback to plain text if marked is not available
            return content.replace(/\n/g, '<br>');
        },

        formatToolName(toolName) {
            if (!toolName) return '';
            // Convert snake_case to Title Case
            return toolName
                .split('_')
                .map(word => word.charAt(0).toUpperCase() + word.slice(1))
                .join(' ');
        },

        async createPlan() {
            if (!this.articleId || this.isCreatingPlan || this.isAiThinking) return;

            this.isCreatingPlan = true;
            this.error = null;

            try {
                // Create conversation if needed
                if (!this.conversationId) {
                    const response = await fetch(`/api/articles/${this.articleId}/assistant/conversation?mode=Plan`, {
                        method: 'POST'
                    });

                    if (!response.ok) {
                        throw new Error('Failed to create conversation');
                    }

                    const conversation = await response.json();
                    this.conversationId = conversation.id;
                    this.mode = 'Plan';
                }

                // Send plan creation request
                const response = await fetch(
                    `/api/articles/${this.articleId}/assistant/conversations/${this.conversationId}/create-plan`,
                    {
                        method: 'POST'
                    }
                );

                if (!response.ok) {
                    const errorData = await response.json();
                    throw new Error(errorData.error || 'Failed to create plan');
                }

                // The plan generation will happen in the background
                // SignalR will notify us when it's ready
                // Set AI thinking state to show processing
                this.isAiThinking = true;

                // Scroll to bottom to show the thinking indicator
                this.$nextTick(() => this.scrollToBottom());
            } catch (err) {
                console.error('Error creating plan:', err);
                this.error = err.message;
            } finally {
                this.isCreatingPlan = false;
            }
        }
    },
    template: `
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
                    <div v-if="!hasMessages && !isAiThinking" class="empty-state" v-cloak>
                        <i class="bi bi-chat-dots empty-state-icon"></i>
                        <p class="empty-state-text">Start a conversation with the AI assistant</p>
                        <p class="empty-state-hint">Ask questions or request improvements to your article</p>
                        <button 
                            @click="createPlan"
                            :disabled="isCreatingPlan || isAiThinking"
                            class="btn btn-primary mt-3">
                            <i class="bi bi-lightbulb"></i>
                            {{ isCreatingPlan ? 'Creating Plan...' : 'Create a Plan' }}
                        </button>
                    </div>

                    <!-- Render grouped messages (turns) -->
                    <template v-for="item in groupedMessages" :key="item.id">
                        <!-- User message -->
                        <div v-if="item.type === 'user'" 
                             class="chat-message chat-message-user">
                            <div class="chat-message-header">
                                <span class="chat-message-author">{{ item.userName }}</span>
                                <span class="chat-message-time">{{ formatDate(item.createdAt) }}</span>
                            </div>
                            <div class="chat-message-body">{{ item.content }}</div>
                        </div>

                        <!-- Assistant turn -->
                        <div v-else-if="item.type === 'assistant-turn'" 
                             class="chat-message chat-message-assistant">
                            <div class="chat-message-header">
                                <span class="chat-message-author">{{ item.latestMessage.userName }}</span>
                                <span class="chat-message-time">{{ formatDate(item.latestMessage.createdAt) }}</span>
                            </div>

                            <div class="chat-message-body">
                                <!-- Show expand button if there are multiple messages -->
                                <div v-if="item.messages.length > 1 && !item.expanded" 
                                     class="chat-turn-expand">
                                    <button 
                                        @click="toggleTurnExpansion(item.id)"
                                        class="btn btn-sm btn-link text-muted">
                                        <i class="bi bi-chevron-down"></i>
                                        Show {{ item.messages.length - 1 }} previous {{ item.messages.length - 1 === 1 ? 'message' : 'messages' }}
                                    </button>
                                </div>

                                <!-- Show previous messages when expanded -->
                                <template v-if="item.messages.length > 1 && item.expanded">
                                    <div v-for="(msg, idx) in item.messages.slice(0, -1)" 
                                         :key="msg.id"
                                         class="chat-turn-previous-message">

                                        <div class="markdown-container"
                                             v-html="renderMarkdown(msg.content)"></div>

                                        <div v-if="msg.toolCalls && msg.toolCalls.length > 0" 
                                             class="chat-message-tools mt-2">
                                            <span v-for="(tool, toolIdx) in msg.toolCalls" 
                                                 :key="toolIdx" 
                                                 class="badge bg-info me-1">
                                                <i v-if="tool.completed" class="bi bi-check-circle me-1"></i>
                                                <span v-else class="spinner-border spinner-border-xs me-1" role="status"></span>
                                                {{ formatToolName(tool.name) }}
                                            </span>
                                        </div>
                                    </div>

                                    <!-- Collapse button -->
                                    <div class="chat-turn-collapse">
                                        <button 
                                            @click="toggleTurnExpansion(item.id)"
                                            class="btn btn-sm btn-link text-muted">
                                            <i class="bi bi-chevron-up"></i>
                                            Hide previous messages
                                        </button>
                                    </div>
                                </template>

                                <!-- Latest message (always shown) -->

                                <div class="markdown-container"
                                     v-html="renderMarkdown(item.latestMessage.content)"></div>

                                <div v-if="item.latestMessage.toolCalls && item.latestMessage.toolCalls.length > 0" 
                                     class="chat-message-tools mt-2">
                                    <span v-for="(tool, idx) in item.latestMessage.toolCalls" 
                                         :key="idx" 
                                         class="badge me-1"
                                         :class="tool.completed ? 'bg-success' : 'bg-secondary'">
                                        <i v-if="tool.completed" class="bi bi-check-circle me-1"></i>
                                        <span v-else class="spinner-border spinner-border-xs me-1" role="status"></span>
                                        {{ formatToolName(tool.name) }}
                                    </span>
                                </div>
                            </div>
                        </div>
                    </template>

                    <div v-if="isAiThinking" class="chat-message chat-message-assistant thinking">
                        <div class="chat-message-header">
                            <span class="chat-message-author">Medley Assistant</span>
                        </div>
                        <div class="chat-message-body">
                            <span class="typing-indicator">
                                <span></span>
                                <span></span>
                                <span></span>
                            </span>
                        </div>
                    </div>
                </div>

                <div v-if="error" class="alert alert-danger alert-sm m-2">
                    {{ error }}
                </div>
 
                <div class="chat-controls px-3 py-2 border-top bg-light d-flex align-items-center justify-content-between">
                    <div class="chat-mode-selector d-flex align-items-center">
                        <label class="small text-muted me-2 mb-0">Mode:</label>
                        <select v-model="mode" 
                                :disabled="isAiThinking"
                                class="form-select form-select-sm" 
                                style="width: auto;">
                            <option value="Chat">Chat</option>
                            <option value="Plan">Plan</option>
                        </select>
                    </div>
                    <button v-if="!conversationId && mode === 'Plan'"
                            @click="createPlan"
                            :disabled="isCreatingPlan || isAiThinking"
                            class="btn btn-primary btn-sm">
                        <i class="bi bi-lightbulb"></i>
                        {{ isCreatingPlan ? 'Creating Plan...' : 'Create Plan' }}
                    </button>
                    <button v-if="conversationId"
                            @click="reset"
                            class="btn btn-link btn-sm text-danger p-0 ms-2"
                            title="Start New Conversation">
                        <i class="bi bi-trash"></i>
                    </button>
                </div>
 
                <div class="chat-input-container">
                    <textarea 
                        v-model="newMessage"
                        @keydown.enter.exact.prevent="sendMessage"
                        :disabled="!articleId || isAiThinking"
                        class="form-control chat-input"
                        placeholder="Ask the AI assistant..."
                        rows="2"></textarea>
                    <button 
                        @click="sendMessage"
                        :disabled="!canSendMessage"
                        class="btn btn-primary btn-sm chat-send-btn">
                        <i class="bi bi-send"></i>
                    </button>
                </div>
            </template>
        </div>
    `
};

// Make component globally available
window.ChatPanel = ChatPanel;
