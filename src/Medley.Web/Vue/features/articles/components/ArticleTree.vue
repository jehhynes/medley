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
              <li><button class="dropdown-item" @click.stop="createChild(article.id); closeDropdown()">New Article</button></li>
            </ul>
          </div>
        </div>
      </div>
      <article-tree 
        v-if="hasChildren(article) && isExpanded(article.id)"
        :articles="article.children"
        :selected-id="selectedId"
        :expanded-ids="expandedIds"
        @select="selectArticle"
        @toggle-expand="toggleExpand"
        @create-child="createChild"
        @edit-article="editArticle"
        @move-article="moveArticle"
        @open-assignment-modal="openAssignmentModal"
        class="tree-children"
      />
    </li>
  </ul>
</template>

<script setup lang="ts">
import { ref, inject } from 'vue';
import { useDropDown } from '@/composables/useDropDown';
import { useArticleView } from '../composables/useArticleView';
import { useArticleTypes } from '../composables/useArticleTypes';
import type { ArticleDto } from '@/types/api-client';

interface DragState {
  draggingArticleId: string | null;
  dragOverId: string | null;
}

interface Props {
  articles: ArticleDto[];
  selectedId: string | null;
  expandedIds: Set<string>;
}

const props = withDefaults(defineProps<Props>(), {
  articles: () => [],
  selectedId: null,
  expandedIds: () => new Set()
});

const { types: articleTypes } = useArticleTypes();

interface Emits {
  (e: 'select', article: ArticleDto): void;
  (e: 'toggle-expand', articleId: string): void;
  (e: 'create-child', articleId: string): void;
  (e: 'edit-article', article: ArticleDto): void;
  (e: 'move-article', sourceId: string, targetId: string): void;
  (e: 'open-assignment-modal', article: ArticleDto): void;
}

const emit = defineEmits<Emits>();

const openAssignmentModal = (article: ArticleDto) => {
  emit('open-assignment-modal', article);
};

const dragState = inject<DragState>('dragState', {
  draggingArticleId: null,
  dragOverId: null
});

const { toggleDropdown, closeDropdown, isDropdownOpen, getPositionClasses } = useDropDown();

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

const dragCounter = ref<number>(0);

const toggleExpand = (articleId: string) => {
  emit('toggle-expand', articleId);
};

const isExpanded = (articleId: string) => {
  return props.expandedIds.has(articleId);
};

const hasChildren = (article: ArticleDto) => {
  return !!(article.children && article.children.length > 0);
};

const isIndexType = (article: ArticleDto) => {
  if (!article.articleTypeId) {
    return false;
  }
  const articleType = articleTypes.value.find(t => t.id === article.articleTypeId);
  return !!(articleType && articleType.name?.toLowerCase() === 'index');
};

const handleDragStart = (event: DragEvent, article: ArticleDto) => {
  if (!article.id) return;
  
  dragState.draggingArticleId = article.id;
  if (event.dataTransfer) {
    event.dataTransfer.effectAllowed = 'move';
    event.dataTransfer.setData('text/plain', article.id);

    const target = event.target as HTMLElement;
    const dragImage = target.cloneNode(true) as HTMLElement;
    dragImage.style.opacity = '0.5';
    event.dataTransfer.setDragImage(target, 0, 0);
  }
};

const handleDragOver = (event: DragEvent, article: ArticleDto) => {
  if (!article.id) return;
  
  if (article.id === dragState.draggingArticleId) {
    return;
  }

  if (!isIndexType(article)) {
    return;
  }

  event.preventDefault();
  event.stopPropagation();
  if (event.dataTransfer) {
    event.dataTransfer.dropEffect = 'move';
  }

  if (dragState.dragOverId !== article.id) {
    dragState.dragOverId = article.id;
    dragCounter.value = 0;
  }
};

const handleDragLeave = (event: DragEvent, article: ArticleDto) => {
  if (!article.id) return;
  
  const target = event.currentTarget as HTMLElement;
  const rect = target.getBoundingClientRect();
  const x = event.clientX;
  const y = event.clientY;

  if (x < rect.left || x >= rect.right || y < rect.top || y >= rect.bottom) {
    if (dragState.dragOverId === article.id) {
      dragState.dragOverId = null;
      dragCounter.value = 0;
    }
  }
};

const handleDrop = (event: DragEvent, targetArticle: ArticleDto) => {
  event.preventDefault();
  event.stopPropagation();

  dragState.dragOverId = null;
  dragCounter.value = 0;

  if (!dragState.draggingArticleId || !targetArticle.id || dragState.draggingArticleId === targetArticle.id) {
    dragState.draggingArticleId = null;
    return;
  }

  if (!isIndexType(targetArticle)) {
    dragState.draggingArticleId = null;
    return;
  }

  emit('move-article', dragState.draggingArticleId, targetArticle.id);

  dragState.draggingArticleId = null;
};

const moveArticle = (sourceId: string, targetId: string) => {
  emit('move-article', sourceId, targetId);
};
</script>
