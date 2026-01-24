<template>
  <div v-if="fragment">
    <!-- Fragment Metadata (Category, Confidence, Date, Source Link, Actions) -->
    <div class="mb-3 d-flex align-items-center justify-content-between">
      <div>
        <span v-if="fragment.category" class="badge bg-secondary me-2">
          <i :class="getIconClass(fragment.categoryIcon, 'bi-puzzle')" class="me-1"></i>{{ fragment.category }}
        </span>
        <span 
          v-if="fragment.confidence !== null && fragment.confidence !== undefined"
          class="badge bg-light text-dark me-2"
          @click="toggleConfidenceComment"
          style="cursor: pointer;"
          :title="showConfidenceComment ? 'Hide confidence note' : 'Show confidence note'">
          <i 
            :class="'fa-duotone ' + getConfidenceIcon(fragment.confidence)" 
            :style="{ color: getConfidenceColor(fragment.confidence) }"
            class="me-1"
          ></i>
          Confidence: {{ fragment.confidence || '' }}
          <i v-if="fragment.confidenceComment" :class="showConfidenceComment ? 'bi bi-chevron-up ms-1' : 'bi bi-chevron-down ms-1'"></i>
        </span>
        <span v-if="fragment.sourceDate" class="text-muted me-2">
          <i class="bi bi-calendar3"></i>
          {{ formatDate(fragment.sourceDate?.toString()) }}
        </span>
        <a v-if="fragment.sourceId" :href="'/Sources?id=' + fragment.sourceId" class="source-link">
          <i class="bi bi-camera-video me-1"></i>{{ fragment.sourceName || 'View Source' }}
        </a>
      </div>
      
      <!-- Actions Dropdown -->
      <div class="dropdown-container">
        <button 
          class="btn btn-sm btn-outline-secondary"
          @click.stop="toggleDropdown($event, 'fragment-actions')"
          title="Actions">
          <i class="bi bi-three-dots"></i>
        </button>
        <ul v-if="dropdownOpen" class="dropdown-menu dropdown-menu-end show">
          <li>
            <button 
              class="dropdown-item text-danger" 
              @click.stop="confirmDelete(); closeDropdown()"
              :disabled="isDeleting">
              <i class="bi bi-trash me-2"></i>
              {{ isDeleting ? 'Deleting...' : 'Delete Fragment' }}
            </button>
          </li>
        </ul>
      </div>
    </div>

    <!-- Confidence Alert with Comment (only show when expanded or editing) -->
    <div v-if="(fragment.confidence !== null && fragment.confidence !== undefined) && (showConfidenceComment || isEditingConfidence)" class="alert alert-info mb-3">
      <div class="d-flex align-items-start justify-content-between">
        <div class="flex-grow-1">
          <!-- Display Mode -->
          <template v-if="!isEditingConfidence">
            <div class="d-flex align-items-center mb-2">
              <i 
                :class="'fa-duotone ' + getConfidenceIcon(fragment.confidence)" 
                :style="{ color: getConfidenceColor(fragment.confidence) }"
                class="me-2"
              ></i>
              <strong>Confidence: {{ fragment.confidence }}</strong>
            </div>
            <div style="white-space: pre-wrap;">{{ fragment.confidenceComment }}</div>
          </template>

          <!-- Edit Mode -->
          <template v-else>
            <div class="mb-2">
              <select 
                id="confidenceLevel"
                v-model="editedConfidence" 
                class="form-select form-select-sm"
                :disabled="isSaving">
                <option :value="null">Select Confidence</option>
                <option value="Unclear">Unclear</option>
                <option value="Low">Low</option>
                <option value="Medium">Medium</option>
                <option value="High">High</option>
                <option value="Certain">Certain</option>
              </select>
            </div>
            <div>
              <textarea 
                id="confidenceComment"
                v-model="editedComment" 
                class="form-control form-control-sm" 
                rows="4"
                placeholder="Add a note explaining your confidence assessment..."
                :disabled="isSaving"
              ></textarea>
            </div>
          </template>
        </div>

        <!-- Action Buttons -->
        <div class="ms-3 d-flex gap-1 flex-shrink-0">
          <template v-if="!isEditingConfidence">
            <button 
              class="btn btn-sm btn-outline-secondary" 
              @click="startEditingConfidence"
              title="Edit confidence">
              <i class="bi bi-pencil"></i>
            </button>
          </template>
          <template v-else>
            <button 
              class="btn btn-sm btn-success" 
              @click="saveConfidence"
              :disabled="isSaving"
              title="Save">
              <span v-if="isSaving" class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span>
              <i v-else class="bi bi-check-lg"></i>
            </button>
            <button 
              class="btn btn-sm btn-outline-secondary" 
              @click="cancelEditingConfidence"
              :disabled="isSaving"
              title="Cancel">
              <i class="bi bi-x-lg"></i>
            </button>
          </template>
        </div>
      </div>
    </div>

    <!-- Add Confidence Button (when no confidence set) -->
    <div v-if="!fragment.confidence && !isEditingConfidence" class="mb-3">
      <button 
        class="btn btn-sm btn-outline-secondary" 
        @click="startEditingConfidence"
        title="Add confidence level">
        <i class="bi bi-plus-circle me-1"></i> Add Confidence Level
      </button>
    </div>

    <!-- Fragment Content -->
    <div class="markdown-container" v-html="renderedMarkdown"></div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import { useDropDown } from '@/composables/useDropDown';
import { 
  getIconClass,
  getConfidenceIcon, 
  getConfidenceColor,
  formatDate
} from '@/utils/helpers';
import { fragmentsClient } from '@/utils/apiClients';
import type { FragmentDto, ConfidenceLevel, UpdateFragmentConfidenceRequest } from '@/types/api-client';

// Declare bootbox as global
declare const bootbox: {
  confirm: (options: {
    title: string;
    message: string;
    buttons: {
      confirm: { label: string; className: string };
      cancel: { label: string; className: string };
    };
    callback: (result: boolean) => void;
    className?: string;
  }) => void;
  alert: (options: {
    title: string;
    message: string;
    buttons: {
      ok: { label: string; className: string };
    };
    className?: string;
  }) => void;
};

// Declare marked as global
declare const marked: {
  parse: (markdown: string) => string;
};

// Props
interface Props {
  fragment: FragmentDto | null;
}

const props = withDefaults(defineProps<Props>(), {
  fragment: null
});

// Emits
interface Emits {
  (e: 'updated', fragment: FragmentDto): void;
  (e: 'deleted', fragmentId: string): void;
}

const emit = defineEmits<Emits>();

// Dropdown management
const { 
  isDropdownOpen, 
  toggleDropdown, 
  closeDropdown 
} = useDropDown();

const dropdownOpen = computed(() => isDropdownOpen('fragment-actions'));

// State
const isEditingConfidence = ref<boolean>(false);
const showConfidenceComment = ref<boolean>(false);
const editedConfidence = ref<ConfidenceLevel | null>(null);
const editedComment = ref<string>('');
const isSaving = ref<boolean>(false);
const hasUnsavedChanges = ref<boolean>(false);
const isDeleting = ref<boolean>(false);

// Computed
const renderedMarkdown = computed<string>(() => {
  if (!props.fragment || !props.fragment.content) {
    return '';
  }
  if (typeof marked !== 'undefined') {
    return marked.parse(props.fragment.content);
  }
  return props.fragment.content.replace(/\n/g, '<br>');
});

// Watchers
watch([editedConfidence, editedComment], () => {
  if (isEditingConfidence.value) {
    hasUnsavedChanges.value = true;
  }
});

// Methods
function toggleConfidenceComment(): void {
  showConfidenceComment.value = !showConfidenceComment.value;
}

function startEditingConfidence(): void {
  if (!props.fragment) return;
  
  editedConfidence.value = props.fragment.confidence ?? null;
  editedComment.value = props.fragment.confidenceComment || '';
  isEditingConfidence.value = true;
  hasUnsavedChanges.value = false;
}

function cancelEditingConfidence(): void {
  if (hasUnsavedChanges.value) {
    bootbox.confirm({
      title: 'Discard Changes',
      message: 'You have unsaved changes to the confidence level. Are you sure you want to discard them?',
      buttons: {
        confirm: {
          label: 'Discard',
          className: 'btn-danger'
        },
        cancel: {
          label: 'Keep Editing',
          className: 'btn-secondary'
        }
      },
      className: 'nested-modal',
      callback: (result: boolean) => {
        if (result) {
          isEditingConfidence.value = false;
          hasUnsavedChanges.value = false;
        }
      }
    });
    return;
  }
  isEditingConfidence.value = false;
  hasUnsavedChanges.value = false;
}

async function saveConfidence(): Promise<void> {
  if (!props.fragment) return;
  
  isSaving.value = true;
  
  try {
    const request: UpdateFragmentConfidenceRequest = {
      confidence: editedConfidence.value,
      confidenceComment: editedComment.value || null
    };
    
    const updatedFragment = await fragmentsClient.updateConfidence(props.fragment.id!, request);
    
    // Show success message
    console.log('Confidence level updated successfully');
    
    isEditingConfidence.value = false;
    hasUnsavedChanges.value = false;
    
    // Emit the updated fragment so parent components can refresh
    emit('updated', updatedFragment);
    
  } catch (error: any) {
    console.error('Error updating confidence:', error);
    bootbox.alert({
      title: 'Error',
      message: 'Failed to update confidence level. Please try again.',
      buttons: {
        ok: {
          label: 'OK',
          className: 'btn-primary'
        }
      },
      className: 'nested-modal'
    });
  } finally {
    isSaving.value = false;
  }
}

async function confirmDelete(): Promise<void> {
  if (!props.fragment) return;
  
  bootbox.confirm({
    title: 'Delete Fragment',
    message: 'Are you sure you want to delete this fragment?<br><br>' +
             '<strong>Note:</strong> Deleted fragments will be hidden from all views and excluded from AI searches.<br><br>' +
             'You cannot delete a fragment that has been merged with another fragment.',
    buttons: {
      confirm: {
        label: '<i class="bi bi-trash me-1"></i> Delete',
        className: 'btn-danger'
      },
      cancel: {
        label: 'Cancel',
        className: 'btn-secondary'
      }
    },
    className: 'nested-modal',
    callback: async (result: boolean) => {
      if (result) {
        await deleteFragment();
      }
    }
  });
}

async function deleteFragment(): Promise<void> {
  if (!props.fragment) return;
  
  isDeleting.value = true;
  
  try {
    const result = await fragmentsClient.deleteFragment(props.fragment.id!);
    
    if (!result.success) {
      bootbox.alert({
        title: 'Cannot Delete Fragment',
        message: result.message || 'Failed to delete fragment. Please try again.',
        buttons: {
          ok: {
            label: 'OK',
            className: 'btn-primary'
          }
        },
        className: 'nested-modal'
      });
      return;
    }
    
    // Success - emit event to parent
    console.log('Fragment deleted successfully');
    emit('deleted', props.fragment.id!);
    
  } catch (error: any) {
    console.error('Error deleting fragment:', error);
    
    // Check if error has a response with a message
    let errorMessage = 'Failed to delete fragment. Please try again.';
    if (error.response) {
      try {
        const errorData = JSON.parse(error.response);
        if (errorData.message) {
          errorMessage = errorData.message;
        }
      } catch (parseError) {
        // Ignore parse errors, use default message
      }
    } else if (error.message) {
      errorMessage = error.message;
    }
    
    bootbox.alert({
      title: 'Error',
      message: errorMessage,
      buttons: {
        ok: {
          label: 'OK',
          className: 'btn-primary'
        }
      },
      className: 'nested-modal'
    });
  } finally {
    isDeleting.value = false;
  }
}

// Expose method for parent to check unsaved changes
defineExpose({
  hasUnsavedChanges: () => hasUnsavedChanges.value
});
</script>
