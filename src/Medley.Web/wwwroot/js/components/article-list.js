// Article List Component - Flat div view for articles with virtual scrolling
const ArticleList = {
    name: 'ArticleList',
    mixins: [window.dropdownMixin],
    components: {
        VirtualScroller
    },
    template: `
  <virtual-scroller 
    :items="articles" 
    :item-height="itemHeight"
    :buffer="buffer"
    key-field="id"
    class="article-list-scroller">
    <template #item="{ item: article }">
      <div 
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
    </template>
  </virtual-scroller>
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
        breadcrumbsCache: {
            type: Map,
            default: () => new Map()
        }
    },
    data() {
        return {
            itemHeight: 52, // Height of each article row in pixels
            buffer: 5 // Number of extra items to render above/below viewport
        };
    },
    methods: {
        selectArticle(article) {
            this.$emit('select', article);
            // Collapse left sidebar on mobile after selection
            window.MedleySidebar?.collapseLeftSidebar();
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
        }
    }
};

