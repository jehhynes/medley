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
                        originalContent: '', // Track original content for dirty checking
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
            computed: {
                hasUnsavedChanges() {
                    return this.articles.selected && 
                           this.editor.content !== this.editor.originalContent;
                }
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
                    // Don't reload if the article is already selected
                    if (article.id === this.articles.selectedId) {
                        return;
                    }

                    // Check for unsaved changes before switching
                    if (this.hasUnsavedChanges) {
                        const shouldProceed = await this.promptUnsavedChanges();
                        if (!shouldProceed) {
                            return;
                        }
                    }

                    try {
                        const fullArticle = await api.get(`/api/articles/${article.id}`);

                        this.editor.title = fullArticle.title;
                        this.editor.content = fullArticle.content || '';
                        this.editor.originalContent = fullArticle.content || ''; // Track for dirty checking
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

                // === Tree Sorting ===
                sortArticles(articles) {
                    articles.sort((a, b) => {
                        // Get article types
                        const aType = this.articles.types.find(t => t.id === a.articleTypeId);
                        const bType = this.articles.types.find(t => t.id === b.articleTypeId);
                        
                        const aIsIndex = aType && aType.name.toLowerCase() === 'index';
                        const bIsIndex = bType && bType.name.toLowerCase() === 'index';
                        
                        // Index types come first
                        if (aIsIndex && !bIsIndex) return -1;
                        if (!aIsIndex && bIsIndex) return 1;
                        
                        // Then sort alphabetically by title (case-insensitive)
                        return a.title.localeCompare(b.title, undefined, { sensitivity: 'base' });
                    });
                },

                sortArticlesRecursive(articles) {
                    // Sort current level
                    this.sortArticles(articles);
                    
                    // Recursively sort children
                    articles.forEach(article => {
                        if (article.children && article.children.length > 0) {
                            this.sortArticlesRecursive(article.children);
                        }
                    });
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

                findParentArray(articleId, articles = this.articles.list) {
                    // Check if article is at root level
                    for (const article of articles) {
                        if (article.id === articleId) {
                            return articles;
                        }
                    }
                    
                    // Search in children
                    for (const article of articles) {
                        if (article.children && article.children.length > 0) {
                            const found = this.findParentArray(articleId, article.children);
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
                        this.sortArticles(this.articles.list);
                    } else {
                        // Find parent and insert into its children
                        const parent = findInTree(this.articles.list, article.parentArticleId);
                        if (parent) {
                            if (!parent.children) {
                                parent.children = [];
                            }
                            parent.children.push(article);
                            this.sortArticles(parent.children);
                            // Expand parent to show new child
                            this.articles.expandedIds.add(parent.id);
                        } else {
                            // Parent not found, insert at root as fallback
                            console.warn(`Parent article ${article.parentArticleId} not found, inserting at root`);
                            this.articles.list.push(article);
                            this.sortArticles(this.articles.list);
                        }
                    }
                },

                updateArticleInTree(articleId, updates) {
                    const article = findInTree(this.articles.list, articleId);
                    if (article) {
                        Object.assign(article, updates);
                        
                        // If title or articleTypeId changed, re-sort the parent array
                        if (updates.title !== undefined || updates.articleTypeId !== undefined) {
                            // Find the parent array that contains this article
                            const parentArray = this.findParentArray(articleId);
                            if (parentArray) {
                                this.sortArticles(parentArray);
                            }
                        }
                        
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
                        // Save content
                        await api.put(`/api/articles/${this.articles.selected.id}/content`, {
                            content: this.editor.content
                        });
                        
                        // Update original content to mark as clean
                        this.editor.originalContent = this.editor.content;
                    } catch (err) {
                        bootbox.alert({
                            message: `Failed to save article: ${err.message}`,
                            className: 'bootbox-error'
                        });
                        console.error('Error saving article:', err);
                    } finally {
                        this.editor.isSaving = false;
                    }
                },

                deleteArticle() {
                    bootbox.confirm({
                        message: 'Are you sure you want to delete this article?',
                        buttons: {
                            confirm: {
                                label: 'Delete',
                                className: 'btn-danger'
                            },
                            cancel: {
                                label: 'Cancel',
                                className: 'btn-secondary'
                            }
                        },
                        callback: (confirmed) => {
                            if (confirmed) {
                                bootbox.alert({
                                    message: 'Delete functionality coming soon',
                                    className: 'bootbox-info'
                                });
                            }
                        }
                    });
                },

                // === UI Helpers ===
                toggleSidebarMenu() {
                    this.ui.sidebarMenuOpen = !this.ui.sidebarMenuOpen;
                },

                formatDate,
                getStatusBadgeClass,

                // === Unsaved Changes ===
                promptUnsavedChanges() {
                    return new Promise((resolve) => {
                        bootbox.dialog({
                            message: 'You have unsaved changes. What would you like to do?',
                            buttons: {
                                save: {
                                    label: '<i class="bi bi-save"></i> Save Changes',
                                    className: 'btn-primary',
                                    callback: async () => {
                                        await this.saveArticle();
                                        resolve(true);
                                    }
                                },
                                discard: {
                                    label: 'Discard Changes',
                                    className: 'btn-outline-danger',
                                    callback: () => {
                                        // Reset content to original
                                        this.editor.content = this.editor.originalContent;
                                        resolve(true);
                                    }
                                },
                                cancel: {
                                    label: 'Cancel',
                                    className: 'btn-secondary',
                                    callback: () => {
                                        resolve(false);
                                    }
                                }
                            }
                        });
                    });
                },

                // === Validation ===
                validateArticleForm(title, typeId) {
                    if (!title?.trim()) {
                        bootbox.alert({
                            message: 'Please enter a title',
                            className: 'bootbox-warning'
                        });
                        return false;
                    }
                    if (!typeId) {
                        bootbox.alert({
                            message: 'Please select an article type',
                            className: 'bootbox-warning'
                        });
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
                        bootbox.alert({
                            message: `Failed to create article: ${err.message}`,
                            className: 'bootbox-error'
                        });
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
                        await api.put(`/api/articles/${this.editModal.articleId}/metadata`, {
                            title: this.editModal.title,
                            articleTypeId: this.editModal.typeId
                        });

                        // Update the article in the tree surgically
                        this.updateArticleInTree(this.editModal.articleId, {
                            title: this.editModal.title,
                            articleTypeId: this.editModal.typeId
                        });

                        // If the article is currently selected, sync the first H1 in the editor
                        if (this.articles.selectedId === this.editModal.articleId) {
                            this.syncFirstHeadingInEditor(this.editModal.title);
                        }

                        this.closeEditModal();
                    } catch (err) {
                        bootbox.alert({
                            message: `Failed to update article: ${err.message}`,
                            className: 'bootbox-error'
                        });
                        console.error('Error updating article:', err);
                    } finally {
                        this.editModal.isSubmitting = false;
                    }
                },

                // Sync the first H1 heading in the TipTap editor
                syncFirstHeadingInEditor(newTitle) {
                    const tiptapEditor = this.$refs.tiptapEditor;
                    if (!tiptapEditor || !tiptapEditor.editor) {
                        return;
                    }

                    const editor = tiptapEditor.editor;
                    
                    // Use ProseMirror to find the first heading level 1
                    let firstH1Pos = null;
                    let firstH1Node = null;
                    
                    editor.state.doc.descendants((node, pos) => {
                        if (firstH1Pos === null && node.type.name === 'heading' && node.attrs.level === 1) {
                            firstH1Pos = pos;
                            firstH1Node = node;
                            return false; // Stop searching
                        }
                    });

                    if (firstH1Pos !== null && firstH1Node !== null) {
                        // Replace the text content of the first H1
                        const from = firstH1Pos + 1; // +1 to get inside the node
                        const to = firstH1Pos + firstH1Node.nodeSize - 1; // -1 to stay inside the node
                        
                        editor.chain()
                            .focus()
                            .setTextSelection({ from, to })
                            .insertContent(newTitle)
                            .run();
                    }
                },

                // === Article Move ===
                async moveArticle(sourceArticleId, targetParentId) {
                    // Find the source and target articles
                    const sourceArticle = findInTree(this.articles.list, sourceArticleId);
                    const targetParent = findInTree(this.articles.list, targetParentId);

                    if (!sourceArticle || !targetParent) {
                        console.error('Source or target article not found');
                        bootbox.alert({
                            message: 'Could not find source or target article',
                            className: 'bootbox-error'
                        });
                        return;
                    }

                    // Show confirmation dialog using bootbox
                    bootbox.confirm({
                        message: `Move <strong>${sourceArticle.title}</strong> under <strong>${targetParent.title}</strong>?`,
                        buttons: {
                            confirm: {
                                label: 'Move',
                                className: 'btn-primary'
                            },
                            cancel: {
                                label: 'Cancel',
                                className: 'btn-secondary'
                            }
                        },
                        callback: async (confirmed) => {
                            if (confirmed) {
                                try {
                                    await api.put(`/api/articles/${sourceArticleId}/move`, {
                                        newParentArticleId: targetParentId
                                    });

                                    // Move completed successfully
                                    // The tree update will be handled by the SignalR event
                                } catch (err) {
                                    bootbox.alert({
                                        message: `Failed to move article: ${err.message}`,
                                        className: 'bootbox-error'
                                    });
                                    console.error('Error moving article:', err);
                                }
                            }
                        }
                    });
                },

                // Helper to move article in tree structure
                moveArticleInTree(articleId, oldParentId, newParentId) {
                    // Find and remove article from old parent
                    let movedArticle = null;

                    const removeFromParent = (articles) => {
                        for (let i = 0; i < articles.length; i++) {
                            if (articles[i].id === articleId) {
                                movedArticle = articles.splice(i, 1)[0];
                                return true;
                            }
                            if (articles[i].children && articles[i].children.length > 0) {
                                if (removeFromParent(articles[i].children)) {
                                    return true;
                                }
                            }
                        }
                        return false;
                    };

                    removeFromParent(this.articles.list);

                    if (!movedArticle) {
                        console.error('Article not found for move:', articleId);
                        return;
                    }

                    // Find new parent and add article to its children
                    const newParent = findInTree(this.articles.list, newParentId);
                    if (newParent) {
                        if (!newParent.children) {
                            newParent.children = [];
                        }
                        newParent.children.push(movedArticle);
                        this.sortArticles(newParent.children);
                        
                        // Expand the new parent to show the moved article
                        this.articles.expandedIds.add(newParentId);
                    } else {
                        console.error('New parent not found:', newParentId);
                        // Add back to root as fallback
                        this.articles.list.push(movedArticle);
                        this.sortArticles(this.articles.list);
                    }
                }
            },

            async mounted() {
                await this.loadArticles();
                await this.loadArticleTypes();
                
                // Sort articles after both articles and types are loaded
                this.sortArticlesRecursive(this.articles.list);

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

                // Add keyboard shortcut for saving (Ctrl+S / Cmd+S)
                this.handleKeyboardSave = (event) => {
                    if ((event.ctrlKey || event.metaKey) && event.key === 's') {
                        event.preventDefault();
                        if (this.articles.selected && !this.editor.isSaving) {
                            this.saveArticle();
                        }
                    }
                };
                document.addEventListener('keydown', this.handleKeyboardSave);

                // Add beforeunload handler to warn about unsaved changes
                this.handleBeforeUnload = (event) => {
                    if (this.hasUnsavedChanges) {
                        event.preventDefault();
                        // Modern browsers require returnValue to be set
                        event.returnValue = 'You have unsaved changes. Are you sure you want to leave?';
                        return event.returnValue;
                    }
                };
                window.addEventListener('beforeunload', this.handleBeforeUnload);

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

                this.hubConnection.on('ArticleMoved', (data) => {
                    // SignalR provides articleId, oldParentId, newParentId
                    // Move the article in the tree surgically
                    this.moveArticleInTree(data.articleId, data.oldParentId, data.newParentId);
                });

                this.hubConnection.start()
                    .then(() => console.log('Connected to ArticleHub'))
                    .catch(err => console.error('SignalR connection error:', err));
            },

            beforeUnmount() {
                // Remove keyboard shortcut listener
                if (this.handleKeyboardSave) {
                    document.removeEventListener('keydown', this.handleKeyboardSave);
                }

                // Remove beforeunload listener
                if (this.handleBeforeUnload) {
                    window.removeEventListener('beforeunload', this.handleBeforeUnload);
                }

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

