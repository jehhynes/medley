// Article List Component - Flat div view for articles
const ArticleList = {
    name: 'ArticleList',
    mixins: [window.dropdownMixin],
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
      <div class="status-actions">
        <i :class="'bi ' + getStatusIcon(article.status) + ' ' + getStatusColorClass(article.status)" class="status-icon" :title="article.status"></i>
        <div class="dropdown actions-container">
          <button 
            class="actions-btn"
            :id="'dropdown-' + article.id"
            data-bs-toggle="dropdown"
            data-bs-auto-close="true"
            aria-expanded="false"
            @click.stop="handleDropdownClick($event, article.id)"
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
        breadcrumbsCache: {
            type: Map,
            default: () => new Map()
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
            return window.MedleyUtils.getStatusIcon(status);
        },
        getStatusColorClass(status) {
            return window.MedleyUtils.getStatusColorClass(status);
        },
        createChild(parentArticleId) {
            this.$emit('create-child', parentArticleId);
        },
        editArticle(article) {
            this.$emit('edit-article', article);
        },
        /**
         * Get breadcrumbs for an article
         * Uses pre-computed breadcrumbs cache for O(1) lookup instead of traversing tree.
         * @param {Object} article - Article to get breadcrumbs for
         * @returns {string|null} Breadcrumb string (e.g., "Parent > Grandparent") or null if root level
         */
        getBreadcrumbs(article) {
            // Use pre-computed breadcrumbs from cache for O(1) lookup
            return this.breadcrumbsCache.get(article.id) || null;
        },
        /**
         * Handle scroll event for infinite scrolling
         * Emits load-more event when user scrolls within 100px of bottom.
         * Debounced to prevent excessive calls.
         * @param {Event} event - DOM scroll event
         */
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
            this.scrollableParent = this.$el.closest('.sidebar-content');
            if (this.scrollableParent) {
                // Debounce scroll handler to avoid too many calls
                this.debouncedScrollHandler = window.MedleyUtils.debounce(this.handleScroll.bind(this), 100);
                this.scrollableParent.addEventListener('scroll', this.debouncedScrollHandler);
            }
        });
    },
    beforeUnmount() {
        if (this.scrollableParent && this.debouncedScrollHandler) {
            this.scrollableParent.removeEventListener('scroll', this.debouncedScrollHandler);
        }
    }
};

