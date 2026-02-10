<template>
  <virtual-scroller 
      :items="flattenedArticles" 
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
            <div v-if="article.breadcrumbs" class="article-breadcrumbs">
              {{ article.breadcrumbs }}
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
import { computed } from 'vue';
import VirtualScroller from '@/components/VirtualScroller.vue';
import { useDropDown } from '@/composables/useDropDown';
import { useArticleView } from '../composables/useArticleView';
import type { ArticleDto } from '@/types/api-client';

interface Props {
  articles: ArticleDto[];
  selectedId: string | null;
}

const props = withDefaults(defineProps<Props>(), {
  articles: () => [],
  selectedId: null
});

interface Emits {
  (e: 'select', article: ArticleDto): void;
  (e: 'edit-article', article: ArticleDto): void;
  (e: 'open-assignment-modal', article: ArticleDto): void;
}

const emit = defineEmits<Emits>();

const openAssignmentModal = (article: ArticleDto) => {
  emit('open-assignment-modal', article);
};

const { toggleDropdown, closeDropdown, isDropdownOpen, getPositionClasses } = useDropDown();

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

interface ArticleWithBreadcrumbs extends ArticleDto {
  breadcrumbs: string | null;
}

const flattenedArticles = computed<ArticleWithBreadcrumbs[]>(() => {
  const flattenArticles = (
    articles: ArticleDto[],
    parentPath: string[] = []
  ): ArticleWithBreadcrumbs[] => {
    let result: ArticleWithBreadcrumbs[] = [];
    for (const article of articles) {
      const breadcrumbs = parentPath.length > 0 
        ? parentPath.join(' > ') 
        : null;

      const articleWithMeta: ArticleWithBreadcrumbs = {
        ...article,
        breadcrumbs
      };
      result.push(articleWithMeta);

      if (article.children && article.children.length > 0) {
        const newPath = [...parentPath, article.title || ''];
        result = result.concat(flattenArticles(article.children, newPath));
      }
    }
    return result;
  };

  const flattened = flattenArticles(props.articles);
  return flattened.sort((a, b) => {
    return (a.title || '').localeCompare(b.title || '', undefined, { sensitivity: 'base' });
  });
});

const itemHeight = 52; // Height of each article row in pixels
const buffer = 5; // Number of extra items to render above/below viewport
</script>
