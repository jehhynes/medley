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
import { computed } from 'vue';
import VirtualScroller from './VirtualScroller.vue';
import { useDropDown } from '@/composables/useDropDown';
import { useArticleView } from '@/composables/useArticleView';
import type { ArticleDto, ArticleTypeDto } from '@/types/generated/api-client';

// Props interface
interface Props {
  articles: ArticleDto[];
  selectedId: string | null;
  articleTypeIconMap: Record<string, string>;
  articleTypes: ArticleTypeDto[];
}

const props = withDefaults(defineProps<Props>(), {
  articles: () => [],
  selectedId: null,
  articleTypeIconMap: () => ({}),
  articleTypes: () => []
});

// Emits interface
interface Emits {
  (e: 'select', article: ArticleDto): void;
  (e: 'edit-article', article: ArticleDto): void;
}

const emit = defineEmits<Emits>();

// Use dropdown composable
const { handleDropdownClick } = useDropDown();

// Use article view composable
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

// Article with breadcrumbs metadata
interface ArticleWithBreadcrumbs extends ArticleDto {
  breadcrumbs: string | null;
}

/**
 * Flatten the article tree into a sorted list with breadcrumbs.
 * Note: Filters are applied server-side via API query params, not here.
 * This just flattens the already-filtered tree from the server.
 */
const flattenedArticles = computed<ArticleWithBreadcrumbs[]>(() => {
  /**
   * Recursively flatten articles with breadcrumbs
   * @param articles - Articles to flatten
   * @param parentPath - Path of parent titles
   * @returns Flattened articles with breadcrumbs
   */
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

// Component data
const itemHeight = 52; // Height of each article row in pixels
const buffer = 5; // Number of extra items to render above/below viewport
</script>
