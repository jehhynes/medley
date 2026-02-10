<template>
  <div v-if="myWorkArticles.length === 0" class="empty-state-small" v-cloak>
    <div class="empty-state-icon">
      <i class="bi bi-person-check"></i>
    </div>
    <p class="text-muted">No articles assigned to you or awaiting your action</p>
  </div>
  <virtual-scroller 
    v-else
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
                :title="article.assignedUser.fullName || 'Assigned User'"
                @click.stop="openAssignmentModal(article)">
            {{ article.assignedUser.initials || '?' }}
          </span>
          <div class="status-icon-wrapper">
            <i :class="getStatusIcon(article.status) + ' ' + getStatusColorClass(article.status)" class="status-icon" :title="article.status"></i>
            <i v-if="showProcessingSpinner(article)" class="fad fa-spinner-third fa-spin status-overlay-spinner text-info" title="AI Processing"></i>
            <span v-if="showUserTurnIndicator(article)" class="status-overlay-badge bg-success" title="Waiting for user"></span>
          </div>
          <div class="dropdown-container actions-container">
            <button 
              class="actions-btn"
              @click.stop="toggleDropdown($event, article.id!)"
              title="Actions">
              <i class="bi bi-three-dots"></i>
            </button>
            <ul v-if="isDropdownOpen(article.id!)" class="dropdown-menu dropdown-menu-end show" :class="getPositionClasses()">
              <li><button class="dropdown-item" @click.stop="editArticle(article); closeDropdown()">Edit</button></li>
              <li><button class="dropdown-item" @click.stop="openAssignmentModal(article); closeDropdown()">Assign</button></li>
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

interface Props {
  articles: ArticleDto[];
  selectedId?: string | null;
}

const props = withDefaults(defineProps<Props>(), {
  articles: () => [],
  selectedId: null
});

// Get current user ID from window
const currentUserId = window.MedleyUser?.id ?? null;

interface Emits {
  (e: 'select', article: ArticleDto): void;
  (e: 'edit-article', article: ArticleDto): void;
  (e: 'open-assignment-modal', article: ArticleDto): void;
}

const emit = defineEmits<Emits>();

const openAssignmentModal = (article: ArticleDto) => {
  emit('open-assignment-modal', article);
};

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
  ref(currentUserId)
);

const { toggleDropdown, closeDropdown, isDropdownOpen, getPositionClasses } = useDropDown();

const itemHeight = ref<number>(52); // Height of each article row in pixels
const buffer = ref<number>(5); // Number of extra items to render above/below viewport

function needsSeparator(index: number): boolean {
  if (index === 0) return false; // No separator before first item
  
  const currentArticle = myWorkArticles.value[index];
  const previousArticle = myWorkArticles.value[index - 1];
  
  const currentIsAiProcessing = showProcessingSpinner(currentArticle);
  const previousIsAiProcessing = showProcessingSpinner(previousArticle);
  
  return !previousIsAiProcessing && currentIsAiProcessing;
}

function formatActivityDate(article: ArticleDto): string {
  const latestDate = getLastActivityDate(article);
  return formatRelativeTime(latestDate, { short: false });
}
</script>
