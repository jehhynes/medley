<template>
  <virtual-scroller 
    :items="articles" 
    :item-height="itemHeight"
    :buffer="buffer"
    key-field="id"
    class="article-list-scroller">
    <template #item="{ item: article, index }">
      <!-- Insert separator before first AI processing item -->
      <hr v-if="needsSeparator(index)" class="my-work-separator" />
      
      <div 
        class="article-list-row"
        :class="{ 'active': selectedId === article.id }"
        @click="selectArticle(article)">
        <div class="article-list-icon">
          <i :class="['article-icon'].concat(getIconClass(getArticleIcon(article)).split(' '))"></i>
        </div>
        <div class="article-list-title">
          <span class="article-title-text">{{ article.title }}</span>
          <div class="article-meta">
            <span class="article-activity-time text-muted small">
              {{ formatRelativeTime(article) }}
            </span>
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
  showUserTurnIndicator,
  formatRelativeTime 
} from '@/utils/helpers.js';

export default {
  name: 'MyWorkList',
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
  emits: ['select', 'edit-article'],
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
    editArticle(article) {
      this.$emit('edit-article', article);
    },
    /**
     * Determine if a separator is needed before this article
     * Shows separator when transitioning to AI processing items
     */
    needsSeparator(index) {
      if (index === 0) return false; // No separator before first item
      
      const currentArticle = this.articles[index];
      const previousArticle = this.articles[index - 1];
      
      const currentIsAiProcessing = showProcessingSpinner(currentArticle);
      const previousIsAiProcessing = showProcessingSpinner(previousArticle);
      
      // Show separator when we transition to AI processing items
      // (previous was not AI processing, current is AI processing)
      return !previousIsAiProcessing && currentIsAiProcessing;
    },
    /**
     * Format the last activity time as relative time
     * @param {Object} article - Article to get activity time for
     * @returns {string} Relative time string (e.g., "2 hours ago")
     */
    formatRelativeTime(article) {
      let latestDate = new Date(article.modifiedAt || article.createdAt || 0);
      
      // Check conversation's last message time if available
      if (article.currentConversation?.lastMessageAt) {
        const conversationDate = new Date(article.currentConversation.lastMessageAt);
        if (conversationDate > latestDate) {
          latestDate = conversationDate;
        }
      }
      
      // Check latest version date if available
      if (article.latestVersionDate) {
        const versionDate = new Date(article.latestVersionDate);
        if (versionDate > latestDate) {
          latestDate = versionDate;
        }
      }
      
      return formatRelativeTime(latestDate, { short: false });
    },
    showProcessingSpinner,
    showUserTurnIndicator
  }
};
</script>
