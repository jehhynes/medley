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
                        typeIconMap: {},
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
                        
                        // Build icon map: articleTypeId -> icon
                        this.articles.typeIconMap = {};
                        this.articles.types.forEach(type => {
                            this.articles.typeIconMap[type.id] = type.icon || 'bi-file-text';
                        });
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

                // === Tree Manipulation ===
                insertArticleIntoTree(article) {
                    // Check if article already exists (prevent duplicates from SignalR)
                    const existing = findInTree(this.articles.list, article.id);
                    if (existing) {
                        return; // Already exists, don't insert again
                    }

                    // Ensure the article has a children array
                    if (!article.children) {
                        article.children = [];
                    }

                    if (!article.parentArticleId) {
                        // Insert at root level
                        this.articles.list.push(article);
                    } else {
                        // Find parent and insert into its children
                        const parent = findInTree(this.articles.list, article.parentArticleId);
                        if (parent) {
                            if (!parent.children) {
                                parent.children = [];
                            }
                            parent.children.push(article);
                            // Expand parent to show new child
                            this.articles.expandedIds.add(parent.id);
                        } else {
                            // Parent not found, insert at root as fallback
                            console.warn(`Parent article ${article.parentArticleId} not found, inserting at root`);
                            this.articles.list.push(article);
                        }
                    }
                },

                updateArticleInTree(articleId, updates) {
                    const article = findInTree(this.articles.list, articleId);
                    if (article) {
                        Object.assign(article, updates);
                        
                        // If currently selected article is being updated, update editor state too
                        if (this.articles.selectedId === articleId) {
                            if (updates.title !== undefined) {
                                this.editor.title = updates.title;
                            }
                            if (updates.content !== undefined) {
                                this.editor.content = updates.content;
                            }
                            // Update selected article reference
                            Object.assign(this.articles.selected, updates);
                        }
                    } else {
                        console.warn(`Article ${articleId} not found in tree for update`);
                    }
                },

                removeArticleFromTree(articleId) {
                    const removeFromArray = (articles) => {
                        for (let i = 0; i < articles.length; i++) {
                            if (articles[i].id === articleId) {
                                articles.splice(i, 1);
                                return true;
                            }
                            if (articles[i].children && articles[i].children.length > 0) {
                                if (removeFromArray(articles[i].children)) {
                                    return true;
                                }
                            }
                        }
                        return false;
                    };

                    removeFromArray(this.articles.list);
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

                        // Update the article in the tree surgically
                        this.updateArticleInTree(this.articles.selected.id, {
                            title: this.editor.title,
                            content: this.editor.content
                        });
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
                        
                        // Insert the new article into the tree surgically
                        this.insertArticleIntoTree(response);

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

                        // Update the article in the tree surgically
                        this.updateArticleInTree(this.editModal.articleId, {
                            title: this.editModal.title,
                            articleTypeId: this.editModal.typeId
                        });

                        this.closeEditModal();
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

                this.hubConnection.on('ArticleCreated', async (data) => {
                    // SignalR provides articleId, title, parentArticleId, articleTypeId
                    // Insert surgically using the data from SignalR
                    const newArticle = {
                        id: data.articleId,
                        title: data.title,
                        parentArticleId: data.parentArticleId,
                        articleTypeId: data.articleTypeId,
                        children: []
                    };
                    this.insertArticleIntoTree(newArticle);
                });

                this.hubConnection.on('ArticleUpdated', async (data) => {
                    // SignalR provides articleId, title, and articleTypeId
                    // Update surgically
                    this.updateArticleInTree(data.articleId, {
                        title: data.title,
                        articleTypeId: data.articleTypeId
                    });
                    
                    // If the updated article is currently selected, refresh its full content
                    if (this.articles.selectedId === data.articleId) {
                        try {
                            const fullArticle = await api.get(`/api/articles/${data.articleId}`);
                            this.editor.title = fullArticle.title;
                            this.editor.content = fullArticle.content || '';
                            Object.assign(this.articles.selected, fullArticle);
                        } catch (err) {
                            console.error('Error refreshing selected article:', err);
                        }
                    }
                });

                this.hubConnection.on('ArticleDeleted', (data) => {
                    // Remove the article from the tree surgically
                    this.removeArticleFromTree(data.articleId);
                    
                    // Clear selection if the deleted article was selected
                    if (this.articles.selectedId === data.articleId) {
                        this.articles.selectedId = null;
                        this.articles.selected = null;
                        this.editor.title = '';
                        this.editor.content = '';
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

