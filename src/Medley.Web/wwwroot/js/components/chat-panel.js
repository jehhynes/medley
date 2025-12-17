// Chat Panel Component - Chat sidebar for articles
const ChatPanel = {
    name: 'ChatPanel',
    template: `
  <div class="d-flex flex-column h-100">
    <div class="sidebar-header">
      <h6 class="sidebar-title">Chat</h6>
    </div>
    <div class="sidebar-content" ref="messagesContainer">
      <div v-if="messages.length === 0" class="empty-state">
        <div class="empty-state-icon">
          <i class="bi bi-chat-dots"></i>
        </div>
        <div class="empty-state-title">No messages yet</div>
        <div class="empty-state-text">Start a conversation about this article</div>
      </div>
      <div v-else class="chat-messages">
        <div v-for="(message, index) in messages" :key="index" class="chat-message">
          <div class="chat-message-header">
            <strong>{{ message.userName }}</strong>
            <span>{{ formatTime(message.timestamp) }}</span>
          </div>
          <div class="chat-message-body">{{ message.text }}</div>
        </div>
      </div>
    </div>
    <div class="sidebar-footer">
      <form @submit.prevent="sendMessage">
        <div class="input-group">
          <input 
            v-model="newMessage" 
            type="text" 
            class="form-control" 
            placeholder="Type a message..."
            :disabled="!articleId">
          <button 
            type="submit" 
            class="btn btn-primary"
            :disabled="!newMessage.trim() || !articleId">
            <i class="bi bi-send"></i>
          </button>
        </div>
      </form>
    </div>
  </div>
    `,
    props: {
        articleId: {
            type: String,
            default: null
        }
    },
    data() {
        return {
            messages: [],
            newMessage: '',
            connection: null,
            isConnected: false
        };
    },
    methods: {
        async sendMessage() {
            if (!this.newMessage.trim() || !this.articleId) return;
            
            const message = {
                text: this.newMessage,
                articleId: this.articleId,
                timestamp: new Date().toISOString(),
                userId: 'current-user',
                userName: 'You'
            };
            
            // Add to local messages immediately
            this.messages.push(message);
            
            // Send via SignalR if connected
            if (this.isConnected && this.connection) {
                try {
                    await this.connection.invoke('SendMessage', this.articleId, this.newMessage);
                } catch (err) {
                    console.error('Error sending message:', err);
                }
            }
            
            this.newMessage = '';
            this.$nextTick(() => {
                this.scrollToBottom();
            });
        },
        scrollToBottom() {
            const container = this.$refs.messagesContainer;
            if (container) {
                container.scrollTop = container.scrollHeight;
            }
        },
        formatTime(timestamp) {
            const date = new Date(timestamp);
            return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
        }
    },
    mounted() {
        // SignalR connection setup placeholder
        // Will be implemented when backend hub is ready
        console.log('Chat panel mounted for article:', this.articleId);
    },
    beforeUnmount() {
        if (this.connection) {
            this.connection.stop();
        }
    }
};

