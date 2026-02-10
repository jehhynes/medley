<template>
  <div class="px-3">
    <div class="d-flex align-items-center justify-content-between mb-3">
      <h6 class="mb-0">Add Review</h6>
      <button type="button" class="btn-close" @click="handleClose" :disabled="submitting" aria-label="Close"></button>
    </div>
    
    <div>
          <!-- Comment Textarea -->
          <div class="mb-3 form-group-overlap">
            <textarea
              ref="commentTextarea"
              v-model="comments"
              class="form-control"
              rows="6"
              placeholder="Leave a comment"
              :disabled="submitting"
              maxlength="2000"
            ></textarea>
            <label>Comment {{ selectedAction === 1 ? '(optional)' : '' }}</label>
          </div>
          
          <!-- Re-assign To -->
          <div class="mb-3 form-group-overlap">
            <select 
              v-model="reassignToUserId" 
              class="form-select"
              :disabled="submitting">
              <option :value="null">Automatic</option>
              <option 
                v-for="user in users" 
                :key="user.id" 
                :value="user.id">
                {{ user.fullName }}
              </option>
            </select>
            <label>Next Action</label>
          </div>
          
          <!-- Review Action Selection -->
          <div class="mb-3">
            
            <div class="form-check">
              <input 
                class="form-check-input" 
                type="radio" 
                name="reviewAction" 
                id="actionComment"
                :value="3"
                v-model="selectedAction"
                :disabled="submitting">
              <label class="form-check-label" for="actionComment">
                Comment
              </label>
            </div>
            
            <div class="form-check">
              <input 
                class="form-check-input" 
                type="radio" 
                name="reviewAction" 
                id="actionApprove"
                :value="1"
                v-model="selectedAction"
                :disabled="submitting">
              <label class="form-check-label" for="actionApprove">
                Approve
              </label>
            </div>
            
            <div class="form-check">
              <input 
                class="form-check-input" 
                type="radio" 
                name="reviewAction" 
                id="actionRequestChanges"
                :value="2"
                v-model="selectedAction"
                :disabled="submitting">
              <label class="form-check-label" for="actionRequestChanges">
                Request changes
              </label>
            </div>
          </div>
          
          <!-- Validation Error -->
          <div v-if="validationError" class="alert alert-danger mt-3 mb-0">
            {{ validationError }}
          </div>
          
          <!-- Submission Error -->
          <div v-if="error" class="alert alert-danger mt-3 mb-0">
            {{ error }}
          </div>
          
          <!-- Success Message -->
          <div v-if="successMessage" class="alert alert-success mt-3 mb-0">
            {{ successMessage }}
          </div>
        
        <div class="d-flex gap-2 mt-3">
          <button 
            type="button" 
            class="btn btn-sm btn-primary" 
            @click="submitReview"
            :disabled="submitting || !canSubmit">
            <span v-if="submitting" class="spinner-border spinner-border-sm me-1" role="status">
              <span class="visually-hidden">Submitting...</span>
            </span>
            {{ submitting ? 'Submitting...' : 'Submit review' }}
          </button>
          <button 
            type="button" 
            class="btn btn-sm btn-secondary" 
            @click="handleClose"
            :disabled="submitting">
            Cancel
          </button>
        </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { apiClients } from '@/utils/apiClients';
import type { ArticleReviewDto, ArticleReviewAction, CreateArticleReviewRequest, UserSummaryDto } from '@/types/api-client';

interface Props {
  articleId: string | null;
}

const props = defineProps<Props>();

interface Emits {
  (e: 'close'): void;
  (e: 'review-submitted', review: ArticleReviewDto, message: string): void;
}

const emit = defineEmits<Emits>();

const comments = ref('');
const selectedAction = ref<ArticleReviewAction>(3); // Default to Comment
const reassignToUserId = ref<string | null>(null);
const submitting = ref(false);
const error = ref<string | null>(null);
const validationError = ref<string | null>(null);
const successMessage = ref<string | null>(null);
const commentTextarea = ref<HTMLTextAreaElement | null>(null);
const users = ref<UserSummaryDto[]>([]);
const currentUserId = (window as any).MedleyUser?.id ?? null;

const commentLength = computed(() => {
  return comments.value.length;
});

onMounted(async () => {
  // Auto-focus the comment textarea when the form is mounted
  commentTextarea.value?.focus();
  
  // Load users for reassignment dropdown
  try {
    const allUsers = await apiClients.users.getAll();
    // Filter out current user
    users.value = allUsers.filter(u => u.id !== currentUserId);
  } catch (err) {
    console.error('Failed to load users:', err);
  }
});

const canSubmit = computed(() => {
  // Comment and Request changes require a comment
  if ((selectedAction.value === 3 || selectedAction.value === 2) && !comments.value.trim()) {
    return false;
  }
  // Comment length must be <= 2000
  if (commentLength.value > 2000) {
    return false;
  }
  return true;
});

function handleClose() {
  if (!submitting.value) {
    emit('close');
  }
}

async function submitReview() {
  if (!props.articleId) return;
  
  validationError.value = null;
  error.value = null;
  
  // Validate - comment is required for Comment and Request Changes actions
  if ((selectedAction.value === 3 || selectedAction.value === 2) && !comments.value.trim()) {
    const actionName = selectedAction.value === 3 ? 'commenting' : 'requesting changes';
    validationError.value = `Comment is required when ${actionName}.`;
    return;
  }
  
  if (commentLength.value > 2000) {
    validationError.value = 'Comment must be 2000 characters or less.';
    return;
  }
  
  submitting.value = true;
  
  try {
    const request: CreateArticleReviewRequest = {
      action: selectedAction.value,
      comments: comments.value.trim() || null,
      reassignToUserId: reassignToUserId.value || undefined
    };
    
    const response = await apiClients.articleReviews.createReview(props.articleId, request);
    
    // Show success message
    successMessage.value = response.message;
    
    // Emit the review to parent
    emit('review-submitted', response.review, response.message);
    
    // Close form after short delay to show success message
    setTimeout(() => {
      emit('close');
    }, 1500);
  } catch (err: any) {
    console.error('Failed to submit review:', err);
    error.value = err.message || 'Failed to submit review. Please try again.';
    submitting.value = false;
  }
}
</script>

