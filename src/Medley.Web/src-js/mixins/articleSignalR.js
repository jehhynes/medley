// Article SignalR Mixin - Handles real-time updates via SignalR
const MAX_QUEUE_SIZE = 100;

export default {
    methods: {
        /**
         * Process queued SignalR updates in batch
         * Batches rapid SignalR events (50ms window) to prevent UI thrashing.
         * Processes all queued updates at once for better performance.
         * Called via debounced handler from SignalR event listeners.
         */
        processSignalRQueue() {
            if (this.signalr.processing || this.signalr.updateQueue.length === 0) {
                return;
            }

            this.signalr.processing = true;

            // Process all queued updates
            const queue = [...this.signalr.updateQueue];
            this.signalr.updateQueue = [];

            queue.forEach(update => {
                switch (update.type) {
                    case 'ArticleCreated':
                        this.insertArticleIntoTree(update.article);
                        break;
                    case 'ArticleUpdated':
                        this.updateArticleInTree(update.articleId, update.updates);
                        break;
                    case 'ArticleAssignmentChanged':
                        this.updateArticleInTree(update.articleId, update.updates);
                        break;
                    case 'ArticleDeleted':
                        this.removeArticleFromTree(update.articleId);
                        if (this.articles.selectedId === update.articleId) {
                            this.articles.selectedId = null;
                            this.articles.selected = null;
                            this.editor.title = '';
                            this.editor.content = '';
                        }
                        break;
                    case 'ArticleMoved':
                        this.moveArticleInTree(update.articleId, update.oldParentId, update.newParentId);
                        break;
                }
            });

            this.signalr.processing = false;
        },

        /**
         * Initialize SignalR connection and event handlers
         * Called during component mount
         */
        initializeSignalRConnection() {
            const { createSignalRConnection } = window.MedleyApi;
            this.signalr.connection = createSignalRConnection('/articleHub');

            // Create debounced processor for SignalR events
            this.processSignalRQueueDebounced = window.MedleyUtils.debounce(() => {
                this.processSignalRQueue();
            }, 50);

            this.signalr.connection.on('ArticleCreated', async (data) => {
                // Queue the update with size limit
                if (this.signalr.updateQueue.length >= MAX_QUEUE_SIZE) {
                    console.warn('SignalR update queue full, dropping oldest updates');
                    this.signalr.updateQueue.shift();
                }
                this.signalr.updateQueue.push({
                    type: 'ArticleCreated',
                    article: {
                        id: data.articleId,
                        title: data.title,
                        parentArticleId: data.parentArticleId,
                        articleTypeId: data.articleTypeId,
                        children: []
                    }
                });
                this.processSignalRQueueDebounced();
            });

            this.signalr.connection.on('ArticleUpdated', async (data) => {
                // Queue the update with size limit
                if (this.signalr.updateQueue.length >= MAX_QUEUE_SIZE) {
                    console.warn('SignalR update queue full, dropping oldest updates');
                    this.signalr.updateQueue.shift();
                }
                this.signalr.updateQueue.push({
                    type: 'ArticleUpdated',
                    articleId: data.articleId,
                    updates: {
                        title: data.title,
                        articleTypeId: data.articleTypeId
                    }
                });
                this.processSignalRQueueDebounced();
            });

            this.signalr.connection.on('ArticleAssignmentChanged', async (data) => {
                // Queue the assignment update with size limit
                if (this.signalr.updateQueue.length >= MAX_QUEUE_SIZE) {
                    console.warn('SignalR update queue full, dropping oldest updates');
                    this.signalr.updateQueue.shift();
                }
                this.signalr.updateQueue.push({
                    type: 'ArticleAssignmentChanged',
                    articleId: data.articleId,
                    updates: {
                        assignedUser: data.userId ? {
                            id: data.userId,
                            fullName: data.userName,
                            initials: data.userInitials,
                            color: data.userColor
                        } : null
                    }
                });
                this.processSignalRQueueDebounced();
            });

            this.signalr.connection.on('ArticleDeleted', (data) => {
                // Queue the deletion with size limit
                if (this.signalr.updateQueue.length >= MAX_QUEUE_SIZE) {
                    console.warn('SignalR update queue full, dropping oldest updates');
                    this.signalr.updateQueue.shift();
                }
                this.signalr.updateQueue.push({
                    type: 'ArticleDeleted',
                    articleId: data.articleId
                });
                this.processSignalRQueueDebounced();
            });

            this.signalr.connection.on('ArticleMoved', (data) => {
                // Queue the move with size limit
                if (this.signalr.updateQueue.length >= MAX_QUEUE_SIZE) {
                    console.warn('SignalR update queue full, dropping oldest updates');
                    this.signalr.updateQueue.shift();
                }
                this.signalr.updateQueue.push({
                    type: 'ArticleMoved',
                    articleId: data.articleId,
                    oldParentId: data.oldParentId,
                    newParentId: data.newParentId
                });
                this.processSignalRQueueDebounced();
            });

            this.signalr.connection.on('VersionCreated', (data) => {
                // SignalR provides articleId, versionId, versionNumber, createdAt
                // Refresh the versions panel if it's for the currently selected article
                if (this.articles.selectedId === data.articleId && this.$refs.versionsPanel) {
                    this.$refs.versionsPanel.loadVersions();
                }
            });

            this.signalr.connection.on('PlanGenerated', async (data) => {
                // Normalize IDs for comparison (handle both string and GUID formats)
                const normalizeId = (id) => id ? id.toString().toLowerCase() : null;
                const selectedId = normalizeId(this.articles.selectedId);
                const eventArticleId = normalizeId(data.articleId);

                console.log('PlanGenerated event received:', {
                    eventArticleId,
                    selectedId,
                    planId: data.planId,
                    matches: selectedId === eventArticleId
                });

                // Open plan tab when plan is generated for the currently selected article
                if (selectedId === eventArticleId) {
                    console.log('Opening plan tab automatically for plan:', data.planId);
                    // Open and switch to the plan tab
                    this.openPlanTab(data.planId);
                } else {
                    console.log('Plan generated for different article, not opening tab');
                }
            });

            this.signalr.connection.on('ChatTurnStarted', (data) => {
                // Update turn status on the article directly
                const article = this.articles.index.get(this.articles.selectedId);
                if (article) {
                    // Ensure currentConversation object exists
                    if (!article.currentConversation) {
                        // Can't assign to property of null, so create the object
                        // We need to use Vue.set or re-assign to trigger reactivity if needed, 
                        // but since articles is reactive, deep assignment works in Vue 3/Proxy
                        article.currentConversation = { id: data.conversationId, isRunning: true };
                    } else {
                        article.currentConversation.isRunning = true;
                        article.currentConversation.id = data.conversationId;
                    }
                }
            });

            this.signalr.connection.on('ChatTurnComplete', (data) => {
                // Update turn status on the article directly
                const article = this.articles.index.get(this.articles.selectedId);
                if (article && article.currentConversation) {
                    article.currentConversation.isRunning = false;
                }
            });

            this.signalr.connection.onreconnected(async (connectionId) => {
                console.log('SignalR reconnected. Re-joining article group.');
                if (this.articles.selectedId) {
                    try {
                        await this.signalr.connection.invoke('JoinArticle', this.articles.selectedId);
                    } catch (err) {
                        console.error('Error re-joining article group after reconnection:', err);
                    }
                }
            });

            return this.signalr.connection.start()
                .then(() => console.log('Connected to ArticleHub'))
                .catch(err => console.error('SignalR connection error:', err));
        },

        /**
         * Disconnect from SignalR
         * Called during component unmount
         */
        disconnectSignalR() {
            if (this.signalr.connection) {
                this.signalr.connection.stop()
                    .then(() => console.log('Disconnected from ArticleHub'))
                    .catch(err => console.error('Error disconnecting from SignalR:', err));
            }
        }
    }
};
