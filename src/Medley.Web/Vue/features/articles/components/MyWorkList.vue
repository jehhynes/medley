<template>
  <virtual-scroller 
    :items="myWorkArticles" 
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
              {{ formatActivityDate(article) }}
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

<script setup lang="ts">
import { ref, toRef } from 'vue';
import VirtualScroller from '@/components/VirtualScroller.vue';
import { useDropDown } from '@/composables/useDropDown';
import { useArticleView } from '../composables/useArticleView';
import { useMyWork } from '../composables/useMyWork';
import { formatRelativeTime } from '@/utils/helpers';
import type { ArticleDto } from '@/types/api-client';

// Props
interface Props {
  articles: ArticleDto[];
  selectedId?: string | null;
  articleTypeIconMap?: Record<string, string>;
  articleTypes?: any[];
  currentUserId?: string | null;
}

const props = withDefaults(defineProps<Props>(), {
  articles: () => [],
  selectedId: null,
  articleTypeIconMap: () => ({}),
  articleTypes: () => [],
  currentUserId: null
});

// Emits
interface Emits {
  (e: 'select', article: ArticleDto): void;
  (e: 'edit-article', article: ArticleDto): void;
}

const emit = defineEmits<Emits>();

// Composables
const {
  selectArticle,
  getArticleIcon,
  editArticle,
  getIconClass,
  getStatusIcon,
  getStatusColorClass,
  showProcessingSpinner,
  showUserTurnIndicator
} = useArticleView(props, emit);

const { myWorkArticles, getLastActivityDate } = useMyWork(
  toRef(props, 'articles'),
  toRef(props, 'currentUserId')
);

const { handleDropdownClick } = useDropDown();

// State
const itemHeight = ref<number>(52); // Height of each article row in pixels
const buffer = ref<number>(5); // Number of extra items to render above/below viewport

// Methods
function needsSeparator(index: number): boolean {
  if (index === 0) return false; // No separator before first item
  
  const currentArticle = myWorkArticles.value[index];
  const previousArticle = myWorkArticles.value[index - 1];
  
  const currentIsAiProcessing = showProcessingSpinner(currentArticle);
  const previousIsAiProcessing = showProcessingSpinner(previousArticle);
  
  // Show separator when we transition to AI processing items
  // (previous was not AI processing, current is AI processing)
  return !previousIsAiProcessing && currentIsAiProcessing;
}

function formatActivityDate(article: ArticleDto): string {
  const latestDate = getLastActivityDate(article);
  return formatRelativeTime(latestDate, { short: false });
}
</script>
