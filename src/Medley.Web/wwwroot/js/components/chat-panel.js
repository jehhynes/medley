// Chat Panel Component - AI-powered article improvement
const ChatPanel = {
    name: 'ChatPanel',
    props: {
        articleId: {
            type: String,
            default: null
        }
    },
    data() {
        return {
            conversationId: null,
            messages: [],
            newMessage: '',
            connection: null,
            isConnected: false,
            isAiThinking: false,
            error: null,
            isLoading: false,
            isCreatingPlan: false
        };
    },
    computed: {
        hasMessages() {
            return this.messages.length > 0;
        },
        canSendMessage() {
            return this.articleId && 
                   this.newMessage.trim() !== '' && 
                   !this.isAiThinking;
        }
    },
    watch: {
        articleId: {
            immediate: true,
            async handler(newId, oldId) {
                if (newId !== oldId) {
                    // Leave old article group if connected
                    if (this.connection && this.connection.state === signalR.HubConnectionState.Connected && oldId) {
                        try {
                            await this.connection.invoke('LeaveArticle', oldId);
                        } catch (err) {
                            console.error('Error leaving article group:', err);
                        }
                    }
                    
                    this.reset();
                    
                    if (newId) {
                        // Join new article group if connected
                        if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
                            try {
                                await this.connection.invoke('JoinArticle', newId);
                            } catch (err) {
                                console.error('Error joining article group:', err);
                            }
                        }
                        this.loadActiveConversation();
                    }
                }
            }
        }
    },
    mounted() {
        this.initializeSignalR();
    },
    beforeUnmount() {
        this.disconnectSignalR();
    },
    methods: {
        async initializeSignalR() {
            try {
                this.connection = new signalR.HubConnectionBuilder()
                    .withUrl('/articleHub')
                    .withAutomaticReconnect()
                    .build();

                // Set up event listeners
                this.connection.on('ChatMessageProcessing', this.onMessageProcessing);
                this.connection.on('ChatMessageReceived', this.onMessageReceived);
                this.connection.on('ChatMessageComplete', this.onMessageComplete);
                this.connection.on('ChatError', this.onChatError);

                await this.connection.start();
                this.isConnected = true;
                console.log('SignalR connected for chat panel');

                // Join article group if we have an article
                if (this.articleId) {
                    await this.connection.invoke('JoinArticle', this.articleId);
                }
            } catch (err) {
                console.error('Error connecting to SignalR:', err);
                this.error = 'Failed to connect to real-time service';
            }
        },

        async disconnectSignalR() {
            if (this.connection) {
                try {
                    if (this.articleId) {
                        await this.connection.invoke('LeaveArticle', this.articleId);
                    }
                    await this.connection.stop();
                } catch (err) {
                    console.error('Error disconnecting SignalR:', err);
                }
            }
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
                        `/api/articles/${this.articleId}/assistant/conversation`,
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
                        body: JSON.stringify({ message: messageText })
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

        onMessageComplete(data) {
            // Verify this is for the current conversation
            if (data.conversationId !== this.conversationId) {
                return;
            }
            
            // Add assistant message to list
            this.messages.push({
                id: data.id,
                role: data.role,
                content: data.content,
                userName: data.userName,
                createdAt: data.createdAt
            });

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

        async createPlan() {
            if (!this.articleId || this.isCreatingPlan || this.isAiThinking) return;

            this.isCreatingPlan = true;
            this.error = null;

            try {
                // Create conversation if needed
                if (!this.conversationId) {
                    const response = await fetch(`/api/articles/${this.articleId}/assistant/conversation`, {
                        method: 'POST'
                    });

                    if (!response.ok) {
                        throw new Error('Failed to create conversation');
                    }

                    const conversation = await response.json();
                    this.conversationId = conversation.id;
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

                    <div v-for="message in messages" 
                         :key="message.id" 
                         class="chat-message"
                         :class="'chat-message-' + message.role">
                        <div class="chat-message-header">
                            <span class="chat-message-author">{{ message.userName }}</span>
                            <span class="chat-message-time">{{ formatDate(message.createdAt) }}</span>
                        </div>
                        <div class="chat-message-body" 
                             :class="{ 'markdown-container': message.role === 'assistant' }"
                             v-if="message.role === 'assistant'"
                             v-html="renderMarkdown(message.content)"></div>
                        <div class="chat-message-body" 
                             v-else>{{ message.content }}</div>
                    </div>

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
