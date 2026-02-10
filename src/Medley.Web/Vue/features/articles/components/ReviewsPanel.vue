<template>
  <div class="d-flex flex-column h-100">
    <div v-if="!showReviewForm" class="p-3 pt-0 border-bottom">
      <button 
        class="btn btn-sm btn-primary" 
        @click="showReviewForm = true"
        :disabled="!articleId">
        <i class="bi bi-plus-lg me-1"></i>
        Add Review
      </button>
    </div>
    
    <review-form
      v-if="showReviewForm"
      :article-id="articleId"
      @close="showReviewForm = false"
      @review-submitted="handleReviewSubmitted"
    />
    
    <div v-if="!showReviewForm" class="flex-grow-1 overflow-auto">
      <div v-if="!articleId" class="text-center text-muted p-4" v-cloak>
        <div class="mb-2">
          <i class="bi bi-chat-left-text" style="font-size: 2rem;"></i>
        </div>
        <div class="fw-semibold">No Article Selected</div>
        <div class="small">Select an article to view its reviews</div>
      </div>
      
      <div v-else-if="loading" class="text-center p-4">
        <div class="spinner-border spinner-border-sm" role="status">
          <span class="visually-hidden">Loading...</span>
        </div>
      </div>
      
      <div v-else-if="error" class="alert alert-danger m-3">
        {{ error }}
      </div>
      
      <div v-else-if="reviews.length === 0" class="text-center text-muted p-4" v-cloak>
        <div class="mb-2">
          <i class="bi bi-chat-left-text" style="font-size: 2rem;"></i>
        </div>
        <div class="fw-semibold">No reviews yet</div>
        <div class="small">Be the first to review this article</div>
      </div>
      
      <div v-else class="p-3">
        <div 
          v-for="review in reviews" 
          :key="review.id" 
          class="border rounded p-3 mb-3">
          <div class="d-flex align-items-center gap-2 mb-2">
            <div class="rounded-circle d-flex align-items-center justify-content-center text-white fw-semibold" 
                 style="width: 32px; height: 32px; font-size: 0.75rem;"
                 :style="{ backgroundColor: review.reviewedBy.color || '#6c757d' }">
              {{ review.reviewedBy.initials || getInitials(review.reviewedBy.fullName) }}
            </div>
            <div class="flex-grow-1">
              <div class="fw-semibold small">{{ review.reviewedBy.fullName }}</div>
              <div class="text-muted" style="font-size: 0.75rem;">{{ formatDate(review.reviewedAt) }}</div>
            </div>
            <span class="badge" :class="getActionBadgeClass(review.action)">
              <i :class="getActionIcon(review.action)" class="me-1"></i>
              {{ getActionLabel(review.action) }}
            </span>
          </div>
          
          <div v-if="review.comments" class="small" style="white-space: pre-wrap;">{{ review.comments }}</div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, watch, computed } from 'vue';
import { apiClients } from '@/utils/apiClients';
import { formatRelativeTime } from '@/utils/helpers';
import type { ArticleReviewDto, ArticleReviewAction } from '@/types/api-client';
import ReviewForm from './ReviewForm.vue';

interface Props {
  articleId: string | null;
}

const props = defineProps<Props>();

interface Emits {
  (e: 'review-added', review: ArticleReviewDto): void;
  (e: 'reviews-loaded', count: number): void;
}

const emit = defineEmits<Emits>();

const reviews = ref<ArticleReviewDto[]>([]);
const loading = ref(false);
const error = ref<string | null>(null);
const showReviewForm = ref(false);

// Watch for article ID changes
watch(() => props.articleId, async (newArticleId) => {
  if (newArticleId) {
    await loadReviews();
  } else {
    reviews.value = [];
    emit('reviews-loaded', 0);
  }
}, { immediate: true });

async function loadReviews() {
  if (!props.articleId) return;
  
  loading.value = true;
  error.value = null;
  
  try {
    reviews.value = await apiClients.articleReviews.getReviews(props.articleId);
    emit('reviews-loaded', reviews.value.length);
  } catch (err: any) {
    console.error('Failed to load reviews:', err);
    error.value = err.message || 'Failed to load reviews';
    emit('reviews-loaded', 0);
  } finally {
    loading.value = false;
  }
}

function handleReviewSubmitted(review: ArticleReviewDto) {
  showReviewForm.value = false;
  // Don't add review here - it will be added via SignalR
  emit('review-added', review);
}

function formatDate(dateString: Date | undefined): string {
  if (!dateString) return '';
  return formatRelativeTime(dateString, { short: false, includeTime: true });
}

function getInitials(fullName: string): string {
  return fullName
    .split(' ')
    .map(n => n[0])
    .join('')
    .toUpperCase()
    .substring(0, 2);
}

function getActionBadgeClass(action: ArticleReviewAction): string {
  const actionStr = String(action);
  switch (actionStr) {
    case 'Approve':
    case '1':
      return 'bg-success';
    case 'RequestChanges':
    case '2':
      return 'bg-warning';
    case 'Comment':
    case '3':
      return 'bg-secondary';
    default:
      return 'bg-secondary';
  }
}

function getActionIcon(action: ArticleReviewAction): string {
  const actionStr = String(action);
  switch (actionStr) {
    case 'Approve':
    case '1':
      return 'bi bi-check-circle-fill';
    case 'RequestChanges':
    case '2':
      return 'bi bi-exclamation-circle-fill';
    case 'Comment':
    case '3':
      return 'bi bi-chat-fill';
    default:
      return 'bi bi-chat-fill';
  }
}

function getActionLabel(action: ArticleReviewAction): string {
  const actionStr = String(action);
  switch (actionStr) {
    case 'Approve':
    case '1':
      return 'Approved';
    case 'RequestChanges':
    case '2':
      return 'Changes Requested';
    case 'Comment':
    case '3':
      return 'Commented';
    default:
      return 'Commented';
  }
}

// Expose method to parent component for real-time updates
defineExpose({
  loadReviews,
  addReview: (review: ArticleReviewDto) => {
    reviews.value.unshift(review);
  }
});
</script>

