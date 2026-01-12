<template>
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
          <span v-if="article.assignedUser" 
                class="user-badge" 
                :style="{ backgroundColor: article.assignedUser.color || '#6c757d' }"
                :title="article.assignedUser.fullName || 'Assigned User'">
            {{ article.assignedUser.initials || '?' }}
          </span>
          <div class="status-icon-wrapper">
            <i :class="getStatusIcon(article.status) + ' ' + getStatusColorClass(article.status)" class="status-icon" :title="article.status"></i>
            <i v-if="showProcessingSpinner(article)" class="fad fa-spinner-third fa-spin status-overlay-spinner text-info" title="AI Processing"></i>
            <span v-if="showUserTurnIndicator(article)" class="status-overlay-badge bg-success" title="Waiting for user"></span>
          </div>
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
</template>

<script>
import VirtualScroller from './VirtualScroller.vue';
import dropdownMixin from '@/mixins/dropdown';
import { 
  getIconClass, 
  getStatusIcon, 
  getStatusColorClass, 
  showProcessingSpinner, 
  showUserTurnIndicator 
} from '@/utils/helpers.js';

export default {
  name: 'ArticleList',
  components: {
    VirtualScroller
  },
  mixins: [dropdownMixin],
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
  emits: ['select', 'create-child', 'edit-article'],
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
    getIconClass,
    getStatusIcon,
    getStatusColorClass,
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
    showProcessingSpinner,
    showUserTurnIndicator
  }
};
</script>
