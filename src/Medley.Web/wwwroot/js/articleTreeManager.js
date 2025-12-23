// Article Tree Manager - Utility class for managing article tree operations
(function () {
    class ArticleTreeManager {
        constructor() {
            this.articleIndex = new Map(); // articleId -> article reference
            this.parentPathCache = new Map(); // articleId -> [{id, title}, ...] parent chain
            this.breadcrumbsCache = new Map(); // articleId -> breadcrumb string
            this.cachedFlatList = []; // Cached flat list of all articles, sorted
        }

        /**
         * Initialize the tree with articles data
         * @param {Array} articles - Root level articles array
         * @param {Array} articleTypes - Article types for sorting
         */
        initialize(articles, articleTypes) {
            this.articles = articles;
            this.articleTypes = articleTypes || [];
            this.rebuildAllCaches();
        }

        /**
         * Rebuild all caches (index, parent paths, breadcrumbs, flat list)
         */
        rebuildAllCaches() {
            this.buildArticleIndex();
            this.buildParentPathCache();
            this.rebuildFlatListCache();
        }

        /**
         * Build the article index map for O(1) lookups
         */
        buildArticleIndex(articles = this.articles) {
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
            this.articleIndex = index;
        }

        /**
         * Build parent path cache and breadcrumbs cache
         */
        buildParentPathCache(articles = this.articles, path = []) {
            articles.forEach(article => {
                // Store the parent path (array of parent objects with id and title)
                this.parentPathCache.set(article.id, [...path]);
                
                // Build breadcrumb string from parent path
                if (path.length > 0) {
                    const breadcrumb = path.map(p => p.title).join(' > ');
                    this.breadcrumbsCache.set(article.id, breadcrumb);
                } else {
                    this.breadcrumbsCache.set(article.id, null);
                }
                
                // Recursively process children
                if (article.children && article.children.length > 0) {
                    this.buildParentPathCache(article.children, [...path, { id: article.id, title: article.title }]);
                }
            });
        }

        /**
         * Rebuild the flat list cache (flattened and sorted)
         */
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
            const flattened = flattenArticles(this.articles);
            this.cachedFlatList = flattened.sort((a, b) => {
                return a.title.localeCompare(b.title, undefined, { sensitivity: 'base' });
            });
        }

        /**
         * Get an article by ID (O(1) lookup)
         */
        getArticle(articleId) {
            return this.articleIndex.get(articleId);
        }

        /**
         * Get parent path for an article
         */
        getParentPath(articleId) {
            return this.parentPathCache.get(articleId) || [];
        }

        /**
         * Get breadcrumbs for an article
         */
        getBreadcrumbs(articleId) {
            return this.breadcrumbsCache.get(articleId);
        }

        /**
         * Get the flat list of articles
         */
        getFlatList() {
            return this.cachedFlatList;
        }

        /**
         * Sort articles array by type (Index first) and then by title
         */
        sortArticles(articles) {
            articles.sort((a, b) => {
                // Get article types
                const aType = this.articleTypes.find(t => t.id === a.articleTypeId);
                const bType = this.articleTypes.find(t => t.id === b.articleTypeId);
                
                const aIsIndex = aType && aType.name.toLowerCase() === 'index';
                const bIsIndex = bType && bType.name.toLowerCase() === 'index';
                
                // Index types come first
                if (aIsIndex && !bIsIndex) return -1;
                if (!aIsIndex && bIsIndex) return 1;
                
                // Then sort alphabetically by title (case-insensitive)
                return a.title.localeCompare(b.title, undefined, { sensitivity: 'base' });
            });
        }

        /**
         * Find the parent array containing an article
         */
        findParentArray(articleId, articles = this.articles) {
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
        }

        /**
         * Insert a new article into the tree
         */
        insertArticle(article) {
            // Check if article already exists (prevent duplicates)
            const existing = this.articleIndex.get(article.id);
            if (existing) {
                return; // Already exists, don't insert again
            }

            // Ensure the article has a children array
            if (!article.children) {
                article.children = [];
            }

            if (!article.parentArticleId) {
                // Insert at root level
                this.articles.push(article);
                this.sortArticles(this.articles);
                // Update caches for root article
                this.parentPathCache.set(article.id, []);
                this.breadcrumbsCache.set(article.id, null);
            } else {
                // Find parent and insert into its children
                const parent = this.articleIndex.get(article.parentArticleId);
                if (parent) {
                    if (!parent.children) {
                        parent.children = [];
                    }
                    parent.children.push(article);
                    this.sortArticles(parent.children);
                    
                    // Update caches for new child
                    const parentPath = this.parentPathCache.get(parent.id) || [];
                    const newPath = [...parentPath, { id: parent.id, title: parent.title }];
                    this.parentPathCache.set(article.id, newPath);
                    const breadcrumb = newPath.map(p => p.title).join(' > ');
                    this.breadcrumbsCache.set(article.id, breadcrumb);
                } else {
                    // Parent not found, insert at root as fallback
                    console.warn(`Parent article ${article.parentArticleId} not found, inserting at root`);
                    this.articles.push(article);
                    this.sortArticles(this.articles);
                    this.parentPathCache.set(article.id, []);
                    this.breadcrumbsCache.set(article.id, null);
                }
            }
            
            // Add to index
            this.articleIndex.set(article.id, article);
            
            // Rebuild flat list cache
            this.rebuildFlatListCache();
        }

        /**
         * Update an article's properties
         */
        updateArticle(articleId, updates) {
            const article = this.articleIndex.get(articleId);
            if (!article) {
                console.warn(`Article ${articleId} not found in tree for update`);
                return false;
            }

            Object.assign(article, updates);
            
            // If title changed, rebuild breadcrumbs for this article's descendants
            if (updates.title !== undefined && article.children && article.children.length > 0) {
                this.buildParentPathCache(article.children, [
                    ...(this.parentPathCache.get(articleId) || []),
                    { id: article.id, title: article.title }
                ]);
            }
            
            // If title or articleTypeId changed, re-sort the parent array
            if (updates.title !== undefined || updates.articleTypeId !== undefined) {
                const parentArray = this.findParentArray(articleId);
                if (parentArray) {
                    this.sortArticles(parentArray);
                }
                // Rebuild flat list cache since sorting changed
                this.rebuildFlatListCache();
            }

            return true;
        }

        /**
         * Remove an article from the tree
         */
        removeArticle(articleId) {
            // Get the article before removing to access its children
            const article = this.articleIndex.get(articleId);
            
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

            removeFromArray(this.articles);
            
            // Remove from caches (including descendants)
            const removeFromCaches = (art) => {
                if (!art) return;
                this.articleIndex.delete(art.id);
                this.parentPathCache.delete(art.id);
                this.breadcrumbsCache.delete(art.id);
                if (art.children && art.children.length > 0) {
                    art.children.forEach(child => removeFromCaches(child));
                }
            };
            
            removeFromCaches(article);
            
            // Rebuild flat list cache
            this.rebuildFlatListCache();
        }

        /**
         * Move an article to a new parent
         */
        moveArticle(articleId, newParentId) {
            let movedArticle = null;

            // Remove from old parent
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

            removeFromParent(this.articles);

            if (!movedArticle) {
                console.error('Article not found for move:', articleId);
                return false;
            }

            // Find new parent and add article to its children
            const newParent = this.articleIndex.get(newParentId);
            if (newParent) {
                if (!newParent.children) {
                    newParent.children = [];
                }
                newParent.children.push(movedArticle);
                this.sortArticles(newParent.children);
                
                // Rebuild parent path cache for the moved article and its descendants
                const parentPath = this.parentPathCache.get(newParentId) || [];
                const newPath = [...parentPath, { id: newParent.id, title: newParent.title }];
                this.buildParentPathCache([movedArticle], newPath);
            } else {
                console.error('New parent not found:', newParentId);
                // Add back to root as fallback
                this.articles.push(movedArticle);
                this.sortArticles(this.articles);
                // Rebuild caches for root
                this.buildParentPathCache([movedArticle], []);
            }
            
            // Rebuild flat list cache
            this.rebuildFlatListCache();
            
            return true;
        }
    }

    // Export to window
    window.ArticleTreeManager = ArticleTreeManager;
})();

