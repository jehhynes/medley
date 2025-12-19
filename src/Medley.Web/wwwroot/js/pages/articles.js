// Articles page Vue app (extracted from Articles/Index.cshtml)
(function () {
    const { createApp } = Vue;
    const { api, createSignalRConnection } = window.MedleyApi;
    const {
        formatDate,
        getStatusBadgeClass,
        findInTree
    } = window.MedleyUtils;
    const {
        getUrlParam,
        setUrlParam
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
                    hubConnection: null,
                    articleTypes: [],
                    showCreateModal: false,
                    newArticleTitle: '',
                    newArticleTypeId: null,
                    parentArticleIdForNew: null,
                    isCreating: false,
                    showSidebarMenu: false,
                    expandedIds: new Set()
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

                        // Expand all parent articles
                        this.expandParents(article.id);

                        const currentId = getUrlParam('id');
                        if (currentId !== article.id) {
                            setUrlParam('id', article.id, replaceState);
                        }
                    } catch (err) {
                        console.error('Error loading article:', err);
                        this.selectedArticle = null;
                    }
                },

                toggleExpand(articleId) {
                    if (this.expandedIds.has(articleId)) {
                        this.expandedIds.delete(articleId);
                    } else {
                        this.expandedIds.add(articleId);
                    }
                },

                findArticleParents(articleId, articles = this.articles, parents = []) {
                    for (const article of articles) {
                        if (article.id === articleId) {
                            return parents;
                        }
                        if (article.children && article.children.length > 0) {
                            const found = this.findArticleParents(articleId, article.children, [...parents, article.id]);
                            if (found) {
                                return found;
                            }
                        }
                    }
                    return null;
                },

                expandParents(articleId) {
                    const parents = this.findArticleParents(articleId);
                    if (parents) {
                        parents.forEach(parentId => {
                            this.expandedIds.add(parentId);
                        });
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

                async loadArticleTypes() {
                    try {
                        this.articleTypes = await api.get('/api/articles/types');
                    } catch (err) {
                        console.error('Error loading article types:', err);
                    }
                },

                toggleSidebarMenu() {
                    this.showSidebarMenu = !this.showSidebarMenu;
                },

                showCreateArticleModal(parentArticleId) {
                    this.showSidebarMenu = false; // Close sidebar menu when opening modal
                    this.parentArticleIdForNew = parentArticleId;
                    this.newArticleTitle = '';
                    this.newArticleTypeId = null;
                    this.showCreateModal = true;
                    
                    // Focus on title input after modal is shown
                    this.$nextTick(() => {
                        if (this.$refs.titleInput) {
                            this.$refs.titleInput.focus();
                        }
                    });
                },

                closeCreateModal() {
                    this.showCreateModal = false;
                    this.newArticleTitle = '';
                    this.newArticleTypeId = null;
                    this.parentArticleIdForNew = null;
                },

                async createArticle() {
                    // Validate inputs
                    if (!this.newArticleTitle.trim()) {
                        alert('Please enter a title');
                        return;
                    }
                    if (!this.newArticleTypeId) {
                        alert('Please select an article type');
                        return;
                    }

                    this.isCreating = true;
                    try {
                        const response = await api.post('/api/articles', {
                            title: this.newArticleTitle,
                            articleTypeId: this.newArticleTypeId,
                            parentArticleId: this.parentArticleIdForNew
                        });

                        // Close modal
                        this.closeCreateModal();

                        // Reload articles tree
                        await this.loadArticles();

                        // Auto-select the newly created article
                        if (response && response.id) {
                            const newArticle = findInTree(this.articles, response.id);
                            if (newArticle) {
                                await this.selectArticle(newArticle, false);
                            }
                        }
                    } catch (err) {
                        alert('Failed to create article: ' + err.message);
                        console.error('Error creating article:', err);
                    } finally {
                        this.isCreating = false;
                    }
                }
            },

            async mounted() {
                await this.loadArticles();
                await this.loadArticleTypes();

                const articleIdFromUrl = getUrlParam('id');
                if (articleIdFromUrl) {
                    const article = findInTree(this.articles, articleIdFromUrl);
                    if (article) {
                        await this.selectArticle(article, true);
                    }
                }

                // Close sidebar menu when clicking outside
                document.addEventListener('click', () => {
                    this.showSidebarMenu = false;
                });

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

