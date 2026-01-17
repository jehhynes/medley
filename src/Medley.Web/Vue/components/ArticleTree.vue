<template>
  <ul class="tree-view">
    <li v-for="article in articles" :key="article.id" class="tree-item">
      <div 
        class="tree-item-content" 
        :class="{ 
          active: selectedId === article.id,
          'drag-over': dragState.dragOverId === article.id
        }" 
        draggable="true"
        @click="selectArticle(article)"
        @dragstart="handleDragStart($event, article)"
        @dragover="handleDragOver($event, article)"
        @dragleave="handleDragLeave($event, article)"
        @drop="handleDrop($event, article)">
        <button 
          v-if="hasChildren(article)" 
          class="tree-item-toggle" 
          :class="{ expanded: isExpanded(article.id) }"
          @click.stop="toggleExpand(article.id)">
          <i class="bi bi-chevron-right"></i>
        </button>
        <span v-else class="tree-item-toggle"></span>
        <i :class="['tree-item-icon'].concat(getIconClass(getArticleIcon(article)).split(' '))"></i>
        <span class="tree-item-label">{{ article.title }}</span>
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
              <li><button class="dropdown-item" @click.stop="createChild(article.id)">New Article</button></li>
            </ul>
          </div>
        </div>
      </div>
      <article-tree 
        v-if="hasChildren(article) && isExpanded(article.id)"
        :articles="article.children"
        :selected-id="selectedId"
        :expanded-ids="expandedIds"
        :article-type-icon-map="articleTypeIconMap"
        :article-types="articleTypes"
        @select="selectArticle"
        @toggle-expand="toggleExpand"
        @create-child="createChild"
        @edit-article="editArticle"
        @move-article="moveArticle"
        class="tree-children"
      />
    </li>
  </ul>
</template>

<script setup lang="ts">
import { ref, inject } from 'vue';
import { useDropDown } from '@/composables/useDropDown';
import { useArticleView } from '@/composables/useArticleView';
import type { ArticleDto, ArticleTypeDto } from '@/types/generated/api-client';

// Drag state interface
interface DragState {
  draggingArticleId: string | null;
  dragOverId: string | null;
}

// Props interface
interface Props {
  articles: ArticleDto[];
  selectedId: string | null;
  expandedIds: Set<string>;
  articleTypeIconMap: Record<string, string>;
  articleTypes: ArticleTypeDto[];
}

const props = withDefaults(defineProps<Props>(), {
  articles: () => [],
  selectedId: null,
  expandedIds: () => new Set(),
  articleTypeIconMap: () => ({}),
  articleTypes: () => []
});

// Emits interface
interface Emits {
  (e: 'select', article: ArticleDto): void;
  (e: 'toggle-expand', articleId: string): void;
  (e: 'create-child', articleId: string): void;
  (e: 'edit-article', article: ArticleDto): void;
  (e: 'move-article', sourceId: string, targetId: string): void;
}

const emit = defineEmits<Emits>();

// Inject drag state from parent
const dragState = inject<DragState>('dragState', {
  draggingArticleId: null,
  dragOverId: null
});

// Use dropdown composable
const { handleDropdownClick } = useDropDown();

// Use article view composable
const {
  selectArticle,
  getArticleIcon,
  editArticle,
  createChild,
  getIconClass,
  getStatusIcon,
  getStatusColorClass,
  showProcessingSpinner,
  showUserTurnIndicator
} = useArticleView(props, emit);

// Component state
const dragCounter = ref<number>(0);

// Methods
const toggleExpand = (articleId: string): void => {
  emit('toggle-expand', articleId);
};

const isExpanded = (articleId: string): boolean => {
  return props.expandedIds.has(articleId);
};

const hasChildren = (article: ArticleDto): boolean => {
  return !!(article.children && article.children.length > 0);
};

/**
 * Check if an article is of type "Index"
 * Index articles can accept other articles as children via drag and drop.
 * @param article - Article to check
 * @returns True if article is an Index type
 */
const isIndexType = (article: ArticleDto): boolean => {
  if (!article.articleTypeId) {
    return false;
  }
  const articleType = props.articleTypes.find(t => t.id === article.articleTypeId);
  return !!(articleType && articleType.name?.toLowerCase() === 'index');
};

/**
 * Handle drag start event
 * Initiates article drag operation and stores dragging article ID in shared state.
 * @param event - DOM drag event
 * @param article - Article being dragged
 */
const handleDragStart = (event: DragEvent, article: ArticleDto): void => {
  if (!article.id) return;
  
  dragState.draggingArticleId = article.id;
  if (event.dataTransfer) {
    event.dataTransfer.effectAllowed = 'move';
    event.dataTransfer.setData('text/plain', article.id);

    // Add a semi-transparent drag image
    const target = event.target as HTMLElement;
    const dragImage = target.cloneNode(true) as HTMLElement;
    dragImage.style.opacity = '0.5';
    event.dataTransfer.setDragImage(target, 0, 0);
  }
};

/**
 * Handle drag over event
 * Validates drop target (must be Index type, not self) and shows visual feedback.
 * @param event - DOM drag event
 * @param article - Article being dragged over (potential drop target)
 */
const handleDragOver = (event: DragEvent, article: ArticleDto): void => {
  if (!article.id) return;
  
  // Don't allow dropping on itself
  if (article.id === dragState.draggingArticleId) {
    return;
  }

  // Only allow dropping on Index type articles
  if (!isIndexType(article)) {
    return;
  }

  event.preventDefault();
  event.stopPropagation();
  if (event.dataTransfer) {
    event.dataTransfer.dropEffect = 'move';
  }

  // Set drag over state
  if (dragState.dragOverId !== article.id) {
    dragState.dragOverId = article.id;
    dragCounter.value = 0;
  }
};

const handleDragLeave = (event: DragEvent, article: ArticleDto): void => {
  if (!article.id) return;
  
  // Only clear drag over state if we're actually leaving the element
  // Use relatedTarget to check if we're entering a child element
  const target = event.currentTarget as HTMLElement;
  const rect = target.getBoundingClientRect();
  const x = event.clientX;
  const y = event.clientY;

  // Check if the mouse is still within the bounds of the element
  if (x < rect.left || x >= rect.right || y < rect.top || y >= rect.bottom) {
    if (dragState.dragOverId === article.id) {
      dragState.dragOverId = null;
      dragCounter.value = 0;
    }
  }
};

/**
 * Handle drop event
 * Completes the drag operation by emitting a move-article event to the parent.
 * Validates drop target and clears drag state.
 * @param event - DOM drop event
 * @param targetArticle - Article receiving the drop (new parent)
 */
const handleDrop = (event: DragEvent, targetArticle: ArticleDto): void => {
  event.preventDefault();
  event.stopPropagation();

  // Clear drag state
  dragState.dragOverId = null;
  dragCounter.value = 0;

  if (!dragState.draggingArticleId || !targetArticle.id || dragState.draggingArticleId === targetArticle.id) {
    dragState.draggingArticleId = null;
    return;
  }

  // Only allow dropping on Index type articles
  if (!isIndexType(targetArticle)) {
    dragState.draggingArticleId = null;
    return;
  }

  // Emit the move event to parent
  emit('move-article', dragState.draggingArticleId, targetArticle.id);

  dragState.draggingArticleId = null;
};

const moveArticle = (sourceId: string, targetId: string): void => {
  // Propagate the event up the tree
  emit('move-article', sourceId, targetId);
};
</script>
