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
                    // Article state
                    articles: {
                        list: [],
                        selected: null,
                        selectedId: null,
                        types: [],
                        expandedIds: new Set()
                    },
                    
                    // Editor state
                    editor: {
                        title: '',
                        content: '',
                        isSaving: false
                    },
                    
                    // Create modal state
                    createModal: {
                        visible: false,
                        title: '',
                        typeId: null,
                        parentId: null,
                        isSubmitting: false
                    },
                    
                    // Edit modal state
                    editModal: {
                        visible: false,
                        articleId: null,
                        title: '',
                        typeId: null,
                        isSubmitting: false
                    },
                    
                    // UI state
                    ui: {
                        loading: false,
                        error: null,
                        sidebarMenuOpen: false
                    },
                    
                    // SignalR
                    hubConnection: null
                };
            },
            methods: {
                // === Article Loading & Selection ===
                async loadArticles() {
                    this.ui.loading = true;
                    this.ui.error = null;
                    try {
                        this.articles.list = await api.get('/api/articles/tree');
                    } catch (err) {
                        this.ui.error = 'Failed to load articles: ' + err.message;
                        console.error('Error loading articles:', err);
                    } finally {
                        this.ui.loading = false;
                    }
                },

                async loadArticleTypes() {
                    try {
                        this.articles.types = await api.get('/api/articles/types');
                    } catch (err) {
                        console.error('Error loading article types:', err);
                    }
                },

                async selectArticle(article, replaceState = false) {
                    try {
                        const fullArticle = await api.get(`/api/articles/${article.id}`);

                        this.editor.title = fullArticle.title;
                        this.editor.content = fullArticle.content || '';
                        this.articles.selected = fullArticle;
                        this.articles.selectedId = article.id;

                        // Expand all parent articles
                        this.expandParents(article.id);

                        const currentId = getUrlParam('id');
                        if (currentId !== article.id) {
                            setUrlParam('id', article.id, replaceState);
                        }
                    } catch (err) {
                        console.error('Error loading article:', err);
                        this.articles.selected = null;
                    }
                },

                // === Tree Navigation ===
                toggleExpand(articleId) {
                    if (this.articles.expandedIds.has(articleId)) {
                        this.articles.expandedIds.delete(articleId);
                    } else {
                        this.articles.expandedIds.add(articleId);
                    }
                },

                findArticleParents(articleId, articles = this.articles.list, parents = []) {
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
                            this.articles.expandedIds.add(parentId);
                        });
                    }
                },

                // === Article Content Editing ===
                async saveArticle() {
                    if (!this.articles.selected) return;

                    this.editor.isSaving = true;
                    try {
                        await api.put(`/api/articles/${this.articles.selected.id}`, {
                            title: this.editor.title,
                            content: this.editor.content
                        });

                        this.articles.selected.title = this.editor.title;
                        this.articles.selected.content = this.editor.content;

                        await this.loadArticles();
                    } catch (err) {
                        alert('Failed to save article: ' + err.message);
                        console.error('Error saving article:', err);
                    } finally {
                        this.editor.isSaving = false;
                    }
                },

                deleteArticle() {
                    if (confirm('Are you sure you want to delete this article?')) {
                        alert('Delete functionality coming soon');
                    }
                },

                // === UI Helpers ===
                toggleSidebarMenu() {
                    this.ui.sidebarMenuOpen = !this.ui.sidebarMenuOpen;
                },

                formatDate,
                getStatusBadgeClass,

                // === Validation ===
                validateArticleForm(title, typeId) {
                    if (!title?.trim()) {
                        alert('Please enter a title');
                        return false;
                    }
                    if (!typeId) {
                        alert('Please select an article type');
                        return false;
                    }
                    return true;
                },

                // === Create Modal ===
                showCreateArticleModal(parentArticleId) {
                    this.ui.sidebarMenuOpen = false;
                    this.createModal.parentId = parentArticleId;
                    this.createModal.title = '';
                    this.createModal.typeId = null;
                    this.createModal.visible = true;
                    
                    this.$nextTick(() => {
                        if (this.$refs.titleInput) {
                            this.$refs.titleInput.focus();
                        }
                    });
                },

                closeCreateModal() {
                    this.createModal.visible = false;
                    this.createModal.title = '';
                    this.createModal.typeId = null;
                    this.createModal.parentId = null;
                },

                async createArticle() {
                    if (!this.validateArticleForm(this.createModal.title, this.createModal.typeId)) {
                        return;
                    }

                    this.createModal.isSubmitting = true;
                    try {
                        const response = await api.post('/api/articles', {
                            title: this.createModal.title,
                            articleTypeId: this.createModal.typeId,
                            parentArticleId: this.createModal.parentId
                        });

                        this.closeCreateModal();
                        await this.loadArticles();

                        // Auto-select the newly created article
                        if (response && response.id) {
                            const newArticle = findInTree(this.articles.list, response.id);
                            if (newArticle) {
                                await this.selectArticle(newArticle, false);
                            }
                        }
                    } catch (err) {
                        alert('Failed to create article: ' + err.message);
                        console.error('Error creating article:', err);
                    } finally {
                        this.createModal.isSubmitting = false;
                    }
                },

                // === Edit Modal ===
                showEditArticleModal(article) {
                    this.editModal.articleId = article.id;
                    this.editModal.title = article.title;
                    this.editModal.typeId = article.articleTypeId || null;
                    this.editModal.visible = true;
                    
                    this.$nextTick(() => {
                        if (this.$refs.editTitleInput) {
                            this.$refs.editTitleInput.focus();
                        }
                    });
                },

                closeEditModal() {
                    this.editModal.visible = false;
                    this.editModal.articleId = null;
                    this.editModal.title = '';
                    this.editModal.typeId = null;
                },

                async updateArticle() {
                    if (!this.validateArticleForm(this.editModal.title, this.editModal.typeId)) {
                        return;
                    }

                    this.editModal.isSubmitting = true;
                    try {
                        await api.put(`/api/articles/${this.editModal.articleId}`, {
                            title: this.editModal.title,
                            articleTypeId: this.editModal.typeId
                        });

                        this.closeEditModal();
                        await this.loadArticles();

                        // If the edited article is currently selected, refresh its data
                        if (this.articles.selectedId === this.editModal.articleId) {
                            const updatedArticle = findInTree(this.articles.list, this.editModal.articleId);
                            if (updatedArticle) {
                                await this.selectArticle(updatedArticle, true);
                            }
                        }
                    } catch (err) {
                        alert('Failed to update article: ' + err.message);
                        console.error('Error updating article:', err);
                    } finally {
                        this.editModal.isSubmitting = false;
                    }
                }
            },

            async mounted() {
                await this.loadArticles();
                await this.loadArticleTypes();

                const articleIdFromUrl = getUrlParam('id');
                if (articleIdFromUrl) {
                    const article = findInTree(this.articles.list, articleIdFromUrl);
                    if (article) {
                        await this.selectArticle(article, true);
                    }
                }

                // Close sidebar menu when clicking outside
                document.addEventListener('click', () => {
                    this.ui.sidebarMenuOpen = false;
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
                    if (this.articles.selectedId === data.articleId) {
                        this.articles.selectedId = null;
                        this.articles.selected = null;
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

