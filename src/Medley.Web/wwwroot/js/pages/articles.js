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
                'article-list': ArticleList,
                'chat-panel': ChatPanel,
                'versions-panel': VersionsPanel,
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
                        expandedIds: new Set(),
                        index: new Map(), // articleId -> article reference for O(1) lookups
                        parentPathCache: new Map(), // articleId -> [{id, title}, ...] parent chain
                        breadcrumbsCache: new Map() // articleId -> breadcrumb string
                    },
                    
                    // View mode state
                    viewMode: 'tree', // 'tree' or 'list'
                    listViewVisibleCount: 50, // Number of items to show in list view (infinite scroll)
                    isLoadingMore: false, // Flag to prevent multiple simultaneous loads
                    
                    // Cached data for performance
                    cachedFlatList: [], // Cached flat list of all articles, sorted
                    
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
                        sidebarMenuOpen: false,
                        activeRightTab: 'chat'
                    },
                    
                    // Version state
                    selectedVersion: null,
                    diffHtml: null,
                    loadingDiff: false,
                    diffError: null,
                    
                    // SignalR
                    hubConnection: null,
                    signalRUpdateQueue: [],
                    processingSignalR: false
                };
            },
            computed: {
                hasUnsavedChanges() {
                    // Delegate to the tiptap editor component
                    return this.$refs.tiptapEditor?.hasChanges || false;
                },
                flatArticlesList() {
                    // Return visible items for infinite scroll from cached flat list
                    return this.cachedFlatList.slice(0, this.listViewVisibleCount);
                },
                hasMoreArticles() {
                    // Check if there are more articles to load
                    return this.listViewVisibleCount < this.cachedFlatList.length;
                }
            },
            methods: {
                // === Article Loading & Selection ===
                async loadArticles() {
                    this.ui.loading = true;
                    this.ui.error = null;
                    try {
                        this.articles.list = await api.get('/api/articles/tree');
                        // Build article index for O(1) lookups
                        this.buildArticleIndex();
                        // Build parent path cache for breadcrumbs
                        this.buildParentPathCache();
                        // Build flat list cache for list view
                        this.rebuildFlatListCache();
                        // Reset visible count when articles are reloaded
                        if (this.viewMode === 'list') {
                            this.listViewVisibleCount = 50;
                        }
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

                buildArticleIndex(articles = this.articles.list) {
                    const index = new Map();
                    const traverse = (items) => {
                        items.forEach(article => {
                            index.set(article.id, article);
                            if (article.children && article.children.length > 0) {
                                traverse(article.children);
                            }
                        });
                    };
                    traverse(articles);
                    this.articles.index = index;
                },

                buildParentPathCache(articles = this.articles.list, path = []) {
                    articles.forEach(article => {
                        // Store the parent path (array of parent objects with id and title)
                        this.articles.parentPathCache.set(article.id, [...path]);
                        
                        // Build breadcrumb string from parent path
                        if (path.length > 0) {
                            const breadcrumb = path.map(p => p.title).join(' > ');
                            this.articles.breadcrumbsCache.set(article.id, breadcrumb);
                        } else {
                            this.articles.breadcrumbsCache.set(article.id, null);
                        }
                        
                        // Recursively process children
                        if (article.children && article.children.length > 0) {
                            this.buildParentPathCache(article.children, [...path, { id: article.id, title: article.title }]);
                        }
                    });
                },

                rebuildFlatListCache() {
                    // Flatten the tree
                    const flattenArticles = (articles, parentId = null) => {
                        let result = [];
                        for (const article of articles) {
                            const articleWithParent = {
                                ...article,
                                parentArticleId: parentId
                            };
                            result.push(articleWithParent);
                            if (article.children && article.children.length > 0) {
                                result = result.concat(flattenArticles(article.children, article.id));
                            }
                        }
                        return result;
                    };
                    
                    // Sort alphabetically by title (case-insensitive)
                    const flattened = flattenArticles(this.articles.list);
                    this.cachedFlatList = flattened.sort((a, b) => {
                        return a.title.localeCompare(b.title, undefined, { sensitivity: 'base' });
                    });
                },
                
                rebuildFlatListCacheDebounced() {
                    // Debounced version to avoid rebuilding multiple times during rapid updates
                    if (!this._debouncedRebuildFlatListCache) {
                        this._debouncedRebuildFlatListCache = window.MedleyUtils.debounce(() => {
                            this.rebuildFlatListCache();
                        }, 100);
                    }
                    this._debouncedRebuildFlatListCache();
                },

                processSignalRQueue() {
                    if (this.processingSignalR || this.signalRUpdateQueue.length === 0) {
                        return;
                    }
                    
                    this.processingSignalR = true;
                    
                    // Process all queued updates
                    const queue = [...this.signalRUpdateQueue];
                    this.signalRUpdateQueue = [];
                    
                    queue.forEach(update => {
                        switch (update.type) {
                            case 'ArticleCreated':
                                this.insertArticleIntoTree(update.article);
                                break;
                            case 'ArticleUpdated':
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
                    
                    this.processingSignalR = false;
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
                        this.articles.selected = fullArticle;
                        this.articles.selectedId = article.id;

                        // Clear version selection when switching articles
                        this.clearVersionSelection();

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
                    const existing = this.articles.index.get(article.id);
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
                        // Update caches for root article
                        this.articles.parentPathCache.set(article.id, []);
                        this.articles.breadcrumbsCache.set(article.id, null);
                    } else {
                        // Find parent and insert into its children
                        const parent = this.articles.index.get(article.parentArticleId);
                        if (parent) {
                            if (!parent.children) {
                                parent.children = [];
                            }
                            parent.children.push(article);
                            this.sortArticles(parent.children);
                            // Expand parent to show new child
                            this.articles.expandedIds.add(parent.id);
                            
                            // Update caches for new child
                            const parentPath = this.articles.parentPathCache.get(parent.id) || [];
                            const newPath = [...parentPath, { id: parent.id, title: parent.title }];
                            this.articles.parentPathCache.set(article.id, newPath);
                            const breadcrumb = newPath.map(p => p.title).join(' > ');
                            this.articles.breadcrumbsCache.set(article.id, breadcrumb);
                        } else {
                            // Parent not found, insert at root as fallback
                            console.warn(`Parent article ${article.parentArticleId} not found, inserting at root`);
                            this.articles.list.push(article);
                            this.sortArticles(this.articles.list);
                            this.articles.parentPathCache.set(article.id, []);
                            this.articles.breadcrumbsCache.set(article.id, null);
                        }
                    }
                    
                    // Add to index
                    this.articles.index.set(article.id, article);
                    
                    // Rebuild flat list cache for list view
                    this.rebuildFlatListCache();
                },

                updateArticleInTree(articleId, updates) {
                    const article = this.articles.index.get(articleId);
                    if (article) {
                        Object.assign(article, updates);
                        
                        // If title changed, rebuild breadcrumbs for this article's descendants
                        if (updates.title !== undefined && article.children && article.children.length > 0) {
                            this.buildParentPathCache(article.children, [
                                ...(this.articles.parentPathCache.get(articleId) || []),
                                { id: article.id, title: article.title }
                            ]);
                        }
                        
                        // If title or articleTypeId changed, re-sort the parent array
                        if (updates.title !== undefined || updates.articleTypeId !== undefined) {
                            // Find the parent array that contains this article
                            const parentArray = this.findParentArray(articleId);
                            if (parentArray) {
                                this.sortArticles(parentArray);
                            }
                            // Rebuild flat list cache since sorting changed
                            this.rebuildFlatListCache();
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
                    // Get the article before removing to access its children
                    const article = this.articles.index.get(articleId);
                    
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
                    
                    // Remove from caches (including descendants)
                    const removeFromCaches = (art) => {
                        if (!art) return;
                        this.articles.index.delete(art.id);
                        this.articles.parentPathCache.delete(art.id);
                        this.articles.breadcrumbsCache.delete(art.id);
                        if (art.children && art.children.length > 0) {
                            art.children.forEach(child => removeFromCaches(child));
                        }
                    };
                    
                    removeFromCaches(article);
                    
                    // Rebuild flat list cache
                    this.rebuildFlatListCache();
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

                setActiveRightTab(tab) {
                    this.ui.activeRightTab = tab;
                },

                formatDate,
                getStatusBadgeClass,

                // === View Mode ===
                setViewMode(mode) {
                    this.viewMode = mode;
                    localStorage.setItem('articlesViewMode', mode);
                    // Reset visible count when switching view modes
                    if (mode === 'list') {
                        this.listViewVisibleCount = 50;
                    }
                },

                // === Infinite Scroll ===
                loadMoreArticles() {
                    if (!this.hasMoreArticles || this.isLoadingMore) {
                        return;
                    }
                    
                    this.isLoadingMore = true;
                    
                    // Use nextTick to ensure reactivity
                    this.$nextTick(() => {
                        // Load 50 more items at a time
                        const newCount = Math.min(
                            this.listViewVisibleCount + 50,
                            this.cachedFlatList.length
                        );
                        
                        if (newCount > this.listViewVisibleCount) {
                            this.listViewVisibleCount = newCount;
                        }
                        
                        this.isLoadingMore = false;
                    });
                },

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
                                        // Just proceed - the content will be replaced when new article loads
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
                            const newArticle = this.articles.index.get(response.id);
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
                    const sourceArticle = this.articles.index.get(sourceArticleId);
                    const targetParent = this.articles.index.get(targetParentId);

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

                // === Version History ===
                async handleVersionSelect(version) {
                    if (!version || !this.articles.selectedId) return;
                    
                    this.selectedVersion = version;
                    this.loadingDiff = true;
                    this.diffError = null;
                    this.diffHtml = null;
                    
                    try {
                        const response = await api.get(
                            `/api/articles/${this.articles.selectedId}/versions/${version.id}/diff`
                        );
                        
                        // Convert markdown to HTML for both versions
                        const beforeHtml = this.markdownToHtml(response.beforeContent || '');
                        const afterHtml = this.markdownToHtml(response.afterContent || '');
                        
                        // Use htmlDiff to compare the HTML versions
                        this.diffHtml = window.HtmlDiff.htmlDiff(beforeHtml, afterHtml);
                    } catch (err) {
                        this.diffError = 'Failed to load diff: ' + err.message;
                        console.error('Error loading diff:', err);
                    } finally {
                        this.loadingDiff = false;
                    }
                },
                
                markdownToHtml(markdown) {
                    if (!markdown) return '';
                    
                    // Use marked library if available
                    if (window.marked) {
                        try {
                            return window.marked.parse(markdown, { 
                                breaks: true, 
                                gfm: true,
                                headerIds: false,
                                mangle: false
                            });
                        } catch (e) {
                            console.error('Failed to parse markdown:', e);
                            return markdown;
                        }
                    }
                    
                    // Fallback: return markdown as-is wrapped in pre
                    return `<pre>${markdown}</pre>`;
                },

                clearVersionSelection() {
                    this.selectedVersion = null;
                    this.diffHtml = null;
                    this.diffError = null;
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
                    const newParent = this.articles.index.get(newParentId);
                    if (newParent) {
                        if (!newParent.children) {
                            newParent.children = [];
                        }
                        newParent.children.push(movedArticle);
                        this.sortArticles(newParent.children);
                        
                        // Rebuild parent path cache for the moved article and its descendants
                        const parentPath = this.articles.parentPathCache.get(newParentId) || [];
                        const newPath = [...parentPath, { id: newParent.id, title: newParent.title }];
                        this.buildParentPathCache([movedArticle], newPath);
                        
                        // Expand the new parent to show the moved article
                        this.articles.expandedIds.add(newParentId);
                    } else {
                        console.error('New parent not found:', newParentId);
                        // Add back to root as fallback
                        this.articles.list.push(movedArticle);
                        this.sortArticles(this.articles.list);
                        // Rebuild caches for root
                        this.buildParentPathCache([movedArticle], []);
                    }
                    
                    // Rebuild flat list cache
                    this.rebuildFlatListCache();
                }
            },

            async mounted() {
                // Load view mode from localStorage
                const savedViewMode = localStorage.getItem('articlesViewMode');
                if (savedViewMode && (savedViewMode === 'tree' || savedViewMode === 'list')) {
                    this.viewMode = savedViewMode;
                }

                // Load article types and articles simultaneously
                await Promise.all([
                    this.loadArticleTypes(),
                    this.loadArticles()
                ]);
                
                // Sort articles after both articles and types are loaded
                this.sortArticlesRecursive(this.articles.list);

                const articleIdFromUrl = getUrlParam('id');
                if (articleIdFromUrl) {
                    const article = this.articles.index.get(articleIdFromUrl);
                    if (article) {
                        await this.selectArticle(article, true);
                    }
                }

                // Close sidebar menu when clicking outside
                document.addEventListener('click', () => {
                    this.ui.sidebarMenuOpen = false;
                });

                // Note: Ctrl+S / Cmd+S keyboard shortcut is now handled by the tiptap editor component

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
                
                // Create debounced processor for SignalR events
                this.processSignalRQueueDebounced = window.MedleyUtils.debounce(() => {
                    this.processSignalRQueue();
                }, 50);

                this.hubConnection.on('ArticleCreated', async (data) => {
                    // Queue the update
                    this.signalRUpdateQueue.push({
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

                this.hubConnection.on('ArticleUpdated', async (data) => {
                    // Queue the update
                    this.signalRUpdateQueue.push({
                        type: 'ArticleUpdated',
                        articleId: data.articleId,
                        updates: {
                            title: data.title,
                            articleTypeId: data.articleTypeId
                        }
                    });
                    this.processSignalRQueueDebounced();
                });

                this.hubConnection.on('ArticleDeleted', (data) => {
                    // Queue the deletion
                    this.signalRUpdateQueue.push({
                        type: 'ArticleDeleted',
                        articleId: data.articleId
                    });
                    this.processSignalRQueueDebounced();
                });

                this.hubConnection.on('ArticleMoved', (data) => {
                    // Queue the move
                    this.signalRUpdateQueue.push({
                        type: 'ArticleMoved',
                        articleId: data.articleId,
                        oldParentId: data.oldParentId,
                        newParentId: data.newParentId
                    });
                    this.processSignalRQueueDebounced();
                });

                this.hubConnection.on('VersionCreated', (data) => {
                    // SignalR provides articleId, versionId, versionNumber, createdAt
                    // Refresh the versions panel if it's for the currently selected article
                    if (this.articles.selectedId === data.articleId && this.$refs.versionsPanel) {
                        this.$refs.versionsPanel.loadVersions();
                    }
                });

                this.hubConnection.start()
                    .then(() => console.log('Connected to ArticleHub'))
                    .catch(err => console.error('SignalR connection error:', err));
            },

            beforeUnmount() {
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

