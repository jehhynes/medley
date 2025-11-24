// Chat Panel Component - Chat sidebar for articles
const ChatPanel = {
    name: 'ChatPanel',
    template: '#chat-panel-template',
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

