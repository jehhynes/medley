// Articles page Vue app (extracted from Articles/Index.cshtml)
(function () {
    const { createApp } = Vue;
    const { api, createSignalRConnection } = window.MedleyApi;
    const {
        formatDate,
        getStatusBadgeClass
    } = window.MedleyUtils;
    const {
        getUrlParam,
        setUrlParam,
        findInTree
    } = window.UrlUtils;

    function waitForTiptapEditor(callback) {
        if (window.TiptapEditor) {
            callback();
        } else {
            setTimeout(() => waitForTiptapEditor(callback), 100);
        }
    }

    function createArticlesApp() {
        const app = createApp({
            components: {
                'article-tree': ArticleTree,
                'chat-panel': ChatPanel,
                'tiptap-editor': window.TiptapEditor
            },
            data() {
                return {
                    articles: [],
                    selectedArticleId: null,
                    selectedArticle: null,
                    loading: false,
                    error: null,
                    editingTitle: '',
                    editingContent: '',
                    isSaving: false,
                    hubConnection: null
                };
            },
            methods: {
                async loadArticles() {
                    this.loading = true;
                    this.error = null;
                    try {
                        this.articles = await api.get('/api/articles/tree');
                    } catch (err) {
                        this.error = 'Failed to load articles: ' + err.message;
                        console.error('Error loading articles:', err);
                    } finally {
                        this.loading = false;
                    }
                },

                async selectArticle(article, replaceState = false) {
                    try {
                        const fullArticle = await api.get(`/api/articles/${article.id}`);

                        this.editingTitle = fullArticle.title;
                        this.editingContent = fullArticle.content || '';
                        this.selectedArticle = fullArticle;
                        this.selectedArticleId = article.id;

                        const currentId = getUrlParam('id');
                        if (currentId !== article.id) {
                            setUrlParam('id', article.id, replaceState);
                        }
                    } catch (err) {
                        console.error('Error loading article:', err);
                        this.selectedArticle = null;
                    }
                },

                formatDate,
                getStatusBadgeClass,

                async saveArticle() {
                    if (!this.selectedArticle) return;

                    this.isSaving = true;
                    try {
                        await api.put(`/api/articles/${this.selectedArticle.id}`, {
                            title: this.editingTitle,
                            content: this.editingContent
                        });

                        this.selectedArticle.title = this.editingTitle;
                        this.selectedArticle.content = this.editingContent;

                        await this.loadArticles();
                    } catch (err) {
                        alert('Failed to save article: ' + err.message);
                        console.error('Error saving article:', err);
                    } finally {
                        this.isSaving = false;
                    }
                },

                deleteArticle() {
                    if (confirm('Are you sure you want to delete this article?')) {
                        alert('Delete functionality coming soon');
                    }
                },

                async createNewArticle() {
                    const title = prompt('Enter article title:');
                    if (title) {
                        try {
                            await api.post('/api/articles', { title });
                            await this.loadArticles();
                        } catch (err) {
                            alert('Failed to create article: ' + err.message);
                        }
                    }
                }
            },

            async mounted() {
                await this.loadArticles();

                const articleIdFromUrl = getUrlParam('id');
                if (articleIdFromUrl) {
                    const article = findInTree(this.articles, articleIdFromUrl);
                    if (article) {
                        await this.selectArticle(article, true);
                    }
                }

                this.hubConnection = createSignalRConnection('/articleHub');

                this.hubConnection.on('ArticleCreated', () => {
                    this.loadArticles();
                });

                this.hubConnection.on('ArticleUpdated', () => {
                    this.loadArticles();
                });

                this.hubConnection.on('ArticleDeleted', (data) => {
                    this.loadArticles();
                    if (this.selectedArticleId === data.articleId) {
                        this.selectedArticleId = null;
                        this.selectedArticle = null;
                    }
                });

                this.hubConnection.start()
                    .then(() => console.log('Connected to ArticleHub'))
                    .catch(err => console.error('SignalR connection error:', err));
            },

            beforeUnmount() {
                if (this.hubConnection) {
                    this.hubConnection.stop()
                        .then(() => console.log('Disconnected from ArticleHub'))
                        .catch(err => console.error('Error disconnecting from SignalR:', err));
                }
            }
        });

        app.mount('#app');
    }

    waitForTiptapEditor(createArticlesApp);
})();

