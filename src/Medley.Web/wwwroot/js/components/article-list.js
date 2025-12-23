// Article List Component - Flat div view for articles
const ArticleList = {
    name: 'ArticleList',
    template: `
  <div class="article-list-container" ref="scrollContainer">
    <div 
      v-for="article in articles" 
      :key="article.id" 
      class="article-list-row"
      :class="{ 'active': selectedId === article.id }"
      @click="selectArticle(article)">
      <div class="article-list-icon">
        <i :class="['article-icon'].concat(getIconClass(getArticleIcon(article)).split(' '))"></i>
      </div>
      <div class="article-list-title">
        <span class="article-title-text">{{ article.title }}</span>
        <div v-if="getBreadcrumbs(article)" class="article-breadcrumbs">
          {{ getBreadcrumbs(article) }}
        </div>
      </div>
      <div class="article-list-status-actions">
        <i :class="'bi ' + getStatusIcon(article.status) + ' ' + getStatusColorClass(article.status)" class="article-status-icon" :title="article.status"></i>
        <div class="dropdown d-inline-block article-actions-container">
          <button 
            class="btn btn-sm btn-link article-actions-btn"
            :id="'dropdown-' + article.id"
            data-bs-toggle="dropdown"
            data-bs-auto-close="true"
            aria-expanded="false"
            @click.stop
            title="Actions">
            <i class="bi bi-three-dots"></i>
          </button>
          <ul class="dropdown-menu dropdown-menu-end" :aria-labelledby="'dropdown-' + article.id">
            <li><button class="dropdown-item" @click.stop="editArticle(article)">Edit</button></li>
          </ul>
        </div>
      </div>
    </div>
    <div v-if="hasMore && articles.length > 0" class="article-list-loading">
      <div class="spinner-border spinner-border-sm" role="status">
        <span class="visually-hidden">Loading...</span>
      </div>
    </div>
  </div>
    `,
    props: {
        articles: {
            type: Array,
            default: () => []
        },
        selectedId: {
            type: String,
            default: null
        },
        articleTypeIconMap: {
            type: Object,
            default: () => ({})
        },
        articleTypes: {
            type: Array,
            default: () => []
        },
        hasMore: {
            type: Boolean,
            default: false
        },
        allArticlesTree: {
            type: Array,
            default: () => []
        }
    },
    methods: {
        selectArticle(article) {
            this.$emit('select', article);
        },
        getArticleIcon(article) {
            // Look up icon from dictionary, fallback to bi-file-text
            if (article.articleTypeId && this.articleTypeIconMap[article.articleTypeId]) {
                return this.articleTypeIconMap[article.articleTypeId];
            }
            return 'bi-file-text';
        },
        getIconClass(icon) {
            return window.MedleyUtils.getIconClass(icon);
        },
        getStatusIcon(status) {
            const iconMap = {
                'Draft': 'bi-pencil',
                'Review': 'bi-eye',
                'Approved': 'bi-check-circle',
                'Archived': 'bi-archive'
            };
            return iconMap[status] || 'bi-circle';
        },
        getStatusColorClass(status) {
            const colorMap = {
                'Draft': 'text-secondary',
                'Review': 'text-info',
                'Approved': 'text-success',
                'Archived': 'text-warning'
            };
            return colorMap[status] || 'text-secondary';
        },
        createChild(parentArticleId) {
            this.$emit('create-child', parentArticleId);
        },
        editArticle(article) {
            this.$emit('edit-article', article);
        },
        findArticleInTree(articleId, articles = this.allArticlesTree) {
            // Recursively find an article by ID in the tree
            for (const article of articles) {
                if (article.id === articleId) {
                    return article;
                }
                if (article.children && article.children.length > 0) {
                    const found = this.findArticleInTree(articleId, article.children);
                    if (found) {
                        return found;
                    }
                }
            }
            return null;
        },
        findParentInTree(articleId, articles = this.allArticlesTree, parentPath = []) {
            // Recursively find an article and return its parent path
            for (const article of articles) {
                if (article.id === articleId) {
                    return parentPath;
                }
                if (article.children && article.children.length > 0) {
                    const found = this.findParentInTree(articleId, article.children, [...parentPath, article]);
                    if (found !== null) {
                        return found;
                    }
                }
            }
            return null;
        },
        getBreadcrumbs(article) {
            if (!article.parentArticleId || !this.allArticlesTree || this.allArticlesTree.length === 0) {
                return null;
            }

            // Find the parent path in the tree
            const parentPath = this.findParentInTree(article.id);
            if (!parentPath || parentPath.length === 0) {
                return null;
            }

            // Get all parents in the chain (full recursive breadcrumbs)
            const breadcrumbTitles = parentPath.map(parent => parent.title);

            return breadcrumbTitles.length > 0 ? breadcrumbTitles.join(' > ') : null;
        },
        handleScroll(event) {
            if (!this.hasMore) return;

            const scrollableElement = event.target;
            
            // Check if user scrolled near the bottom (within 100px)
            const scrollTop = scrollableElement.scrollTop;
            const scrollHeight = scrollableElement.scrollHeight;
            const clientHeight = scrollableElement.clientHeight;
            const distanceFromBottom = scrollHeight - scrollTop - clientHeight;

            if (distanceFromBottom < 100) {
                this.$emit('load-more');
            }
        }
    },
    mounted() {
        // Find the actual scrollable container (sidebar-content)
        // Use $el to get the root element and find the scrollable parent
        this.$nextTick(() => {
            const scrollableParent = this.$el.closest('.sidebar-content');
            if (scrollableParent) {
                // Debounce scroll handler to avoid too many calls
                this.debouncedScrollHandler = window.MedleyUtils.debounce(this.handleScroll.bind(this), 100);
                scrollableParent.addEventListener('scroll', this.debouncedScrollHandler);
                this.scrollableParent = scrollableParent;
            }
        });
    },
    beforeUnmount() {
        if (this.scrollableParent && this.debouncedScrollHandler) {
            this.scrollableParent.removeEventListener('scroll', this.debouncedScrollHandler);
        }
    }
};

