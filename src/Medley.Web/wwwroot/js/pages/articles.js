// Articles page Vue app (extracted from Articles/Index.cshtml)
(function () {
    const { createApp } = Vue;
    const { api, createSignalRConnection } = window.MedleyApi;
    const {
        getArticleTypes,
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
            mixins: [
                window.articleModalMixin,
                window.articleVersionMixin,
                window.articleSignalRMixin,
                window.articleFilterMixin
            ],
            components: {
                'article-tree': ArticleTree,
                'article-list': ArticleList,
                'chat-panel': ChatPanel,
                'versions-panel': VersionsPanel,
                'plan-viewer': window.PlanViewer,
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
                        typeIndexMap: {}, // typeId -> type object for O(1) lookups
                        expandedIds: new Set(),
                        index: new Map(), // articleId -> article reference for O(1) lookups
                        parentPathCache: new Map(), // articleId -> [{id, title}, ...] parent chain
                        breadcrumbsCache: new Map() // articleId -> breadcrumb string
                    },

                    // View mode state
                    viewMode: 'tree', // 'tree' or 'list'

                    // Cached data for performance
                    cachedFlatList: [], // Cached flat list of all articles, sorted

                    // Editor state
                    editor: {
                        title: '',
                        content: '',
                        isSaving: false
                    },

                    // Content tabs state
                    contentTabs: {
                        activeTabId: 'editor',
                        versionData: null, // { versionId, versionNumber, createdAt, createdByName, loadingDiff, diffError, diffHtml }
                        planData: null // { planId }
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
                        activeRightTab: 'assistant'
                    },

                    // Version state
                    version: {
                        selected: null,
                        diffHtml: null,
                        loadingDiff: false,
                        diffError: null
                    },

                    // SignalR state
                    signalr: {
                        connection: null,
                        updateQueue: [],
                        processing: false
                    },

                    // Drag state (for article-tree component)
                    dragState: {
                        draggingArticleId: null,
                        dragOverId: null
                    }
                };
            },
            provide() {
                return {
                    dragState: this.dragState
                };
            },
            computed: {
                hasUnsavedChanges() {
                    // Delegate to the tiptap editor component
                    return this.$refs.tiptapEditor?.hasChanges || false;
                },
                flatArticlesList() {
                    // Return all articles from cached flat list for virtual scrolling
                    return this.cachedFlatList;
                },
                availableTabs() {
                    // Build array of available tabs based on state
                    const tabs = [{ id: 'editor', label: 'Editor', closeable: false, type: 'editor' }];

                    if (this.contentTabs.versionData) {
                        tabs.push({
                            id: 'version',
                            label: `Version ${this.contentTabs.versionData.versionNumber}`,
                            closeable: true,
                            type: 'version'
                        });
                    }

                    if (this.contentTabs.planData) {
                        tabs.push({
                            id: 'plan',
                            label: 'Plan',
                            closeable: true,
                            type: 'plan'
                        });
                    }

                    return tabs;
                }
            },
            methods: {
                // === Article Loading & Selection ===
                /**
                 * Load all articles from the API and build caches
                 * Builds article index, parent path cache, breadcrumbs cache, and flat list cache
                 * @returns {Promise<void>}
                 * @throws {Error} If API call fails
                 */
                async loadArticles() {
                    this.ui.loading = true;
                    this.ui.error = null;
                    try {
                        const queryString = this.buildFilterQueryString();
                        this.articles.list = await api.get(`/api/articles/tree${queryString}`);
                        // Build article index for O(1) lookups
                        this.buildArticleIndex();
                        // Build parent path cache for breadcrumbs
                        this.buildParentPathCache();
                        // Build flat list cache for list view
                        this.rebuildFlatListCache();
                    } catch (err) {
                        this.ui.error = 'Failed to load articles: ' + err.message;
                        console.error('Error loading articles:', err);
                    } finally {
                        this.ui.loading = false;
                    }
                },

                /**
                 * Load article types from the API and build type caches
                 * Builds typeIconMap (typeId -> icon) and typeIndexMap (typeId -> type object)
                 * @returns {Promise<void>}
                 */
                async loadArticleTypes() {
                    try {
                        // Use global article types loader
                        this.articles.types = await getArticleTypes();

                        // Build icon map and type index: articleTypeId -> icon/type
                        this.articles.typeIconMap = {};
                        this.articles.typeIndexMap = {};
                        this.articles.types.forEach(type => {
                            this.articles.typeIconMap[type.id] = type.icon || 'bi-file-text';
                            this.articles.typeIndexMap[type.id] = type;
                        });
                    } catch (err) {
                        console.error('Error loading article types:', err);
                        window.MedleyUtils.showToast('error', 'Failed to load article types');
                    }
                },

                /**
                 * Build article index map for O(1) lookups
                 * Traverses the entire tree and creates a Map of articleId -> article object references.
                 * This eliminates the need for O(n) tree traversals when looking up articles.
                 * @param {Array} articles - Root level articles array (defaults to this.articles.list)
                 */
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

                /**
                 * Build parent path cache and breadcrumbs cache
                 * Pre-calculates breadcrumbs for all articles to avoid expensive tree traversals during rendering.
                 * Each article gets a parent path array and a pre-computed breadcrumb string for O(1) lookups.
                 * @param {Array} articles - Articles array to process (defaults to root level)
                 * @param {Array} path - Current parent path (used internally for recursion)
                 */
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

                /**
                 * Rebuild flat list cache for list view
                 * Flattens the hierarchical tree into a sorted array for efficient list view rendering.
                 * Called when tree structure changes (create, delete, move, update with title change).
                 * Prevents expensive re-computation on every render.
                 * Applies client-side filtering for article types and statuses to exclude parent items that don't match filters.
                 */
                rebuildFlatListCache() {
                    // Flatten the tree with article type and status filtering
                    const flattenArticles = (articles, parentId = null) => {
                        let result = [];
                        for (const article of articles) {
                            // Apply article type filter if active
                            const matchesArticleType = this.filters.articleTypeIds.length === 0 ||
                                this.filters.articleTypeIds.includes(article.articleTypeId);

                            // Apply status filter if active
                            const matchesStatus = this.filters.statuses.length === 0 ||
                                this.filters.statuses.includes(article.status);

                            // Include article only if it matches both filters
                            const shouldInclude = matchesArticleType && matchesStatus;

                            if (shouldInclude) {
                                const articleWithParent = {
                                    ...article,
                                    parentArticleId: parentId
                                };
                                result.push(articleWithParent);
                            }

                            // Always recurse into children regardless of parent inclusion
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

                /**
                 * Select an article and load its full content
                 * Checks for unsaved changes, loads full article data, expands parents in tree, and updates URL
                 * @param {Object} article - Article object to select (must have id property)
                 * @param {boolean} replaceState - Whether to replace browser history state (default: false)
                 * @returns {Promise<void>}
                 */
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

                        // Leave the previous article's SignalR group if we had one selected
                        if (this.articles.selectedId && this.signalr.connection && this.signalr.connection.state === signalR.HubConnectionState.Connected) {
                            await this.signalr.connection.invoke('LeaveArticle', this.articles.selectedId);
                        }

                        this.editor.title = fullArticle.title;
                        this.editor.content = fullArticle.content || '';
                        this.articles.selected = fullArticle;
                        this.articles.selectedId = article.id;

                        // Join the new article's SignalR group
                        if (this.signalr.connection && this.signalr.connection.state === signalR.HubConnectionState.Connected) {
                            await this.signalr.connection.invoke('JoinArticle', article.id);
                        }

                        // Clear version selection when switching articles
                        this.clearVersionSelection();

                        // Clear all tabs when switching articles
                        this.clearAllTabs();

                        // Load draft plan if exists
                        await this.loadDraftPlan(article.id);

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
                        // Get article types from cached index for O(1) lookup
                        const aType = this.articles.typeIndexMap[a.articleTypeId];
                        const bType = this.articles.typeIndexMap[b.articleTypeId];

                        const aIsIndex = aType?.name.toLowerCase() === 'index';
                        const bIsIndex = bType?.name.toLowerCase() === 'index';

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

                getArticleParents(articleId) {
                    // Use cached parent path for O(1) lookup
                    const parentPath = this.articles.parentPathCache.get(articleId);
                    return parentPath ? parentPath.map(p => p.id) : [];
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
                    const parents = this.getArticleParents(articleId);
                    if (parents) {
                        parents.forEach(parentId => {
                            this.articles.expandedIds.add(parentId);
                        });
                    }
                },

                // === Tree Manipulation ===
                /**
                 * Insert a new article into the tree
                 * Surgically adds the article to the correct location and updates all caches.
                 * Optimized to only rebuild affected caches (parent path, breadcrumbs, flat list).
                 * @param {Object} article - Article object to insert
                 */
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

                /**
                 * Update an article's properties in the tree
                 * Selectively rebuilds only affected caches based on what changed.
                 * - Title change: Rebuilds breadcrumbs for descendants only
                 * - Title/Type change: Re-sorts parent array and flat list
                 * - Other changes: No cache rebuild (object reference remains valid)
                 * @param {string} articleId - ID of article to update
                 * @param {Object} updates - Properties to update
                 */
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
                /**
                 * Save the currently selected article's content
                 * Updates article content via API and handles errors with user notification
                 * @param {number} retryCount - Number of retry attempts (for auto-save retry logic)
                 * @returns {Promise<void>}
                 */
                async saveArticle(retryCount = 0) {
                    if (!this.articles.selected) return;

                    this.editor.isSaving = true;
                    try {
                        // Save content
                        const response = await api.put(`/api/articles/${this.articles.selected.id}/content`, {
                            content: this.editor.content
                        });

                        // Log version info for debugging (silent for auto-save)
                        if (response && response.versionNumber) {
                            console.log(`Article saved - Version ${response.versionNumber} (${response.isNewVersion ? 'new' : 'updated'})`);
                        }
                    } catch (err) {
                        console.error('Error saving article:', err);

                        // Retry logic for auto-save (max 3 attempts with exponential backoff)
                        if (retryCount < 3) {
                            const delay = Math.pow(2, retryCount) * 1000; // 1s, 2s, 4s
                            console.log(`Retrying save in ${delay}ms (attempt ${retryCount + 1}/3)...`);
                            await new Promise(resolve => setTimeout(resolve, delay));
                            return this.saveArticle(retryCount + 1);
                        }

                        // After 3 failed attempts, still fail silently but log it
                        console.error('Failed to save article after 3 attempts');
                    } finally {
                        this.editor.isSaving = false;
                    }
                },

                deleteArticle() {
                    bootbox.confirm({
                        title: 'Delete Article',
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
                                    title: 'Delete Article',
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
                },

                toggleViewMode() {
                    const newMode = this.viewMode === 'tree' ? 'list' : 'tree';
                    this.setViewMode(newMode);
                },

                // === Unsaved Changes ===
                promptUnsavedChanges() {
                    return new Promise((resolve) => {
                        bootbox.dialog({
                            title: 'Unsaved Changes',
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

                // === Article Move ===
                /**
                 * Move an article to a new parent via drag and drop
                 * Shows confirmation dialog before moving, updates via API, and handles SignalR update
                 * @param {string} sourceArticleId - ID of article to move
                 * @param {string} targetParentId - ID of new parent article (must be Index type)
                 * @returns {Promise<void>}
                 */
                async moveArticle(sourceArticleId, targetParentId) {
                    // Find the source and target articles
                    const sourceArticle = this.articles.index.get(sourceArticleId);
                    const targetParent = this.articles.index.get(targetParentId);

                    if (!sourceArticle || !targetParent) {
                        console.error('Source or target article not found');
                        bootbox.alert({
                            title: 'Move Article',
                            message: 'Could not find source or target article',
                            className: 'bootbox-error'
                        });
                        return;
                    }

                    // Show confirmation dialog using bootbox
                    bootbox.confirm({
                        title: 'Move Article',
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
                                        title: 'Move Article',
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
                },

                // === Content Tab Management ===
                switchContentTab(tabId) {
                    this.contentTabs.activeTabId = tabId;
                },

                closeContentTab(tabId) {
                    if (tabId === 'version') {
                        this.contentTabs.versionData = null;
                        this.contentTabs.activeTabId = 'editor';
                    } else if (tabId === 'plan') {
                        this.contentTabs.planData = null;
                        this.contentTabs.activeTabId = 'editor';
                    }
                },

                openPlanTab(planId) {
                    // Set or update plan data (reuses the single plan tab)
                    this.contentTabs.planData = { planId };
                    this.contentTabs.activeTabId = 'plan';
                },

                openVersionTab(version) {
                    // Set or update version data (reuses the single version tab)
                    this.contentTabs.versionData = {
                        versionId: version.id,
                        versionNumber: version.versionNumber,
                        createdAt: version.createdAt,
                        createdByName: version.createdByName,
                        loadingDiff: true,
                        diffError: null,
                        diffHtml: null
                    };

                    this.contentTabs.activeTabId = 'version';

                    // Load the diff
                    this.loadVersionDiff();
                },

                async loadVersionDiff() {
                    if (!this.contentTabs.versionData) return;

                    try {
                        const response = await api.get(
                            `/api/articles/${this.articles.selectedId}/versions/${this.contentTabs.versionData.versionId}/diff`
                        );

                        // Convert markdown to HTML for both versions
                        const beforeHtml = this.markdownToHtml(response.beforeContent || '');
                        const afterHtml = this.markdownToHtml(response.afterContent || '');

                        // Use htmlDiff to compare the HTML versions
                        this.contentTabs.versionData.diffHtml = window.HtmlDiff.htmlDiff(beforeHtml, afterHtml);
                        this.contentTabs.versionData.loadingDiff = false;
                    } catch (err) {
                        this.contentTabs.versionData.diffError = 'Failed to load diff: ' + err.message;
                        this.contentTabs.versionData.loadingDiff = false;
                        console.error('Error loading diff:', err);
                    }
                },

                async loadDraftPlan(articleId) {
                    try {
                        const response = await api.get(`/api/articles/${articleId}/plans/active`);

                        // If a draft plan exists, open it
                        if (response && response.id) {
                            this.openPlanTab(response.id);
                        }
                    } catch (err) {
                        // 204 No Content means no draft plan exists, which is fine
                        if (err.response && err.response.status === 204) {
                            return;
                        }
                        console.error('Error loading draft plan:', err);
                    }
                },

                clearAllTabs() {
                    // Reset tabs to just editor
                    this.contentTabs.versionData = null;
                    this.contentTabs.planData = null;
                    this.contentTabs.activeTabId = 'editor';
                }
            },

            async mounted() {
                // Load view mode from localStorage
                const savedViewMode = localStorage.getItem('articlesViewMode');
                if (savedViewMode && (savedViewMode === 'tree' || savedViewMode === 'list')) {
                    this.viewMode = savedViewMode;
                }

                // Initialize SignalR connection first and wait for it to connect (from mixin)
                await this.initializeSignalRConnection();

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
            },

            beforeUnmount() {
                // Remove beforeunload listener
                if (this.handleBeforeUnload) {
                    window.removeEventListener('beforeunload', this.handleBeforeUnload);
                }

                // Disconnect from SignalR (from mixin)
                this.disconnectSignalR();
            }
        });

        app.mount('#app');
    }

    waitForTiptapEditor(createArticlesApp);
})();

