<template>
  <div v-if="knowledgeUnit" class="d-flex flex-column h-100">
    <!-- Header -->
    <div class="main-content-header">
      <div class="d-flex justify-content-between align-items-start mb-3">
        <div>
          <h1 class="main-content-title">{{ knowledgeUnit.title || 'Untitled Knowledge Unit' }}</h1>
          <div class="text-muted">
            <span v-if="knowledgeUnit.category" class="badge bg-secondary me-2">
              <i :class="getIconClass(knowledgeUnit.categoryIcon, 'fal fa-atom')" class="me-1"></i>{{ knowledgeUnit.category }}
            </span>
            <span 
              v-if="knowledgeUnit.confidence"
              class="badge bg-light text-dark me-2"
              @click="toggleConfidenceComment"
              style="cursor: pointer;"
              :title="showConfidenceComment ? 'Hide confidence note' : 'Show confidence note'">
              <i 
                :class="'fa-duotone ' + getConfidenceIcon(knowledgeUnit.confidence)" 
                :style="{ color: getConfidenceColor(knowledgeUnit.confidence) }"
                class="me-1"
              ></i>
              Confidence: {{ knowledgeUnit.confidence }}
              <i v-if="knowledgeUnit.confidenceComment" :class="showConfidenceComment ? 'bi bi-chevron-up ms-1' : 'bi bi-chevron-down ms-1'"></i>
            </span>
            <span class="me-2">
              <i class="bi bi-calendar3"></i>
              {{ formatDate(knowledgeUnit.updatedAt?.toString()) }}
            </span>
            <span class="me-2">
              <i class="bi bi-puzzle"></i>
              {{ knowledgeUnit.fragmentCount }} fragment{{ knowledgeUnit.fragmentCount !== 1 ? 's' : '' }}
            </span>
          </div>
        </div>
        
        <!-- Actions Dropdown -->
        <div class="dropdown-container">
          <button 
            class="btn btn-primary"
            @click.stop="toggleDropdown($event, 'knowledge-unit-actions')"
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
                {{ isDeleting ? 'Deleting...' : 'Delete Knowledge Unit' }}
              </button>
            </li>
          </ul>
        </div>
      </div>

      <!-- Confidence Alert with Comment (only show when expanded or editing) -->
      <div v-if="knowledgeUnit.confidence && (showConfidenceComment || isEditingConfidence)" class="alert alert-info mb-3">
        <div class="d-flex align-items-start justify-content-between">
          <div class="flex-grow-1">
            <!-- Display Mode -->
            <template v-if="!isEditingConfidence">
              <div class="d-flex align-items-center mb-2">
                <i 
                  :class="'fa-duotone ' + getConfidenceIcon(knowledgeUnit.confidence)" 
                  :style="{ color: getConfidenceColor(knowledgeUnit.confidence) }"
                  class="me-2"
                ></i>
                <strong>Confidence: {{ knowledgeUnit.confidence }}</strong>
              </div>
              <div style="white-space: pre-wrap;">{{ knowledgeUnit.confidenceComment }}</div>
            </template>

            <!-- Edit Mode -->
            <template v-else>
              <div class="mb-2">
                <select 
                  id="confidenceLevel"
                  v-model="editedConfidence" 
                  class="form-select form-select-sm"
                  :disabled="isSaving">
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
    </div>

    <!-- Tabs -->
    <ul class="nav nav-tabs" role="tablist">
      <li class="nav-item" role="presentation">
        <button 
          class="nav-link" 
          :class="{ active: activeTab === 'content' }"
          @click="activeTab = 'content'"
          type="button" 
          role="tab">
          Content
        </button>
      </li>
      <li class="nav-item" role="presentation">
        <button 
          class="nav-link" 
          :class="{ active: activeTab === 'fragments' }"
          @click="activeTab = 'fragments'"
          type="button" 
          role="tab">
          Fragments ({{ knowledgeUnit.fragmentCount }})
        </button>
      </li>
    </ul>

    <!-- Tab Content -->
    <div class="tab-content">
      <!-- Content Tab -->
      <div class="tab-pane" :class="{ active: activeTab === 'content' }">
        <div class="markdown-container" v-html="renderedMarkdown"></div>
      </div>

      <!-- Fragments Tab -->
      <div class="tab-pane" :class="{ active: activeTab === 'fragments' }">
        <!-- Clustering Comment (if present) -->
        <div v-if="knowledgeUnit.clusteringComment" class="alert alert-secondary mb-3">
          <div class="d-flex align-items-start">
            <i class="bi bi-info-circle me-2 mt-1"></i>
            <div class="flex-grow-1">
              <strong>Clustering Note:</strong>
              <div class="mt-1" style="white-space: pre-wrap;">{{ knowledgeUnit.clusteringComment }}</div>
            </div>
          </div>
        </div>

        <fragment-table
          :fragments="fragments"
          :loading="loadingFragments"
          :error="fragmentsError"
          :show-source-date="true"
          :show-source-speaker="true"
          empty-message="No fragments are linked to this knowledge unit."
          @select-fragment="selectFragment"
        />
      </div>
    </div>

    <!-- Fragment Detail Modal -->
    <fragment-modal
      :fragment="selectedFragment"
      :visible="!!selectedFragment"
      @close="closeFragmentModal"
      @updated="handleFragmentUpdated"
      @deleted="handleFragmentDeleted"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue';
import { useDropDown } from '@/composables/useDropDown';
import { 
  getIconClass,
  getConfidenceIcon, 
  getConfidenceColor,
  formatDate,
  showToast
} from '@/utils/helpers';
import { knowledgeUnitsClient, fragmentsClient } from '@/utils/apiClients';
import type { KnowledgeUnitDto, ConfidenceLevel, UpdateKnowledgeUnitConfidenceRequest, FragmentDto } from '@/types/api-client';
import FragmentTable from '@/components/FragmentTable.vue';
import FragmentModal from './FragmentModal.vue';

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
  knowledgeUnit: KnowledgeUnitDto | null;
}

const props = withDefaults(defineProps<Props>(), {
  knowledgeUnit: null
});

// Emits
interface Emits {
  (e: 'updated', knowledgeUnit: KnowledgeUnitDto): void;
  (e: 'deleted', knowledgeUnitId: string): void;
}

const emit = defineEmits<Emits>();

// Dropdown management
const { 
  isDropdownOpen, 
  toggleDropdown, 
  closeDropdown 
} = useDropDown();

const dropdownOpen = computed(() => isDropdownOpen('knowledge-unit-actions'));

// State
const isEditingConfidence = ref<boolean>(false);
const showConfidenceComment = ref<boolean>(false);
const editedConfidence = ref<ConfidenceLevel>('Medium');
const editedComment = ref<string>('');
const isSaving = ref<boolean>(false);
const hasUnsavedChanges = ref<boolean>(false);
const isDeleting = ref<boolean>(false);
const activeTab = ref<string>('content');
const fragments = ref<FragmentDto[]>([]);
const selectedFragment = ref<FragmentDto | null>(null);
const loadingFragments = ref<boolean>(false);
const fragmentsError = ref<string | null>(null);

// Computed
const renderedMarkdown = computed<string>(() => {
  if (!props.knowledgeUnit || !props.knowledgeUnit.content) {
    return '';
  }
  if (typeof marked !== 'undefined') {
    return marked.parse(props.knowledgeUnit.content);
  }
  return props.knowledgeUnit.content.replace(/\n/g, '<br>');
});

// Watchers
watch([editedConfidence, editedComment], () => {
  if (isEditingConfidence.value) {
    hasUnsavedChanges.value = true;
  }
});

watch(() => props.knowledgeUnit, async (newVal) => {
  fragments.value = [];
  selectedFragment.value = null;
  fragmentsError.value = null;

  if (newVal) {
    await loadFragments();
  }
});

// Lifecycle
onMounted(async () => {
  if (props.knowledgeUnit) {
    await loadFragments();
  }
});

// Methods
function toggleConfidenceComment(): void {
  showConfidenceComment.value = !showConfidenceComment.value;
}

function startEditingConfidence(): void {
  if (!props.knowledgeUnit) return;
  
  editedConfidence.value = props.knowledgeUnit.confidence;
  editedComment.value = props.knowledgeUnit.confidenceComment || '';
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
  if (!props.knowledgeUnit) return;
  
  isSaving.value = true;
  
  try {
    const request: UpdateKnowledgeUnitConfidenceRequest = {
      confidence: editedConfidence.value,
      confidenceComment: editedComment.value || null
    };
    
    const updatedKnowledgeUnit = await knowledgeUnitsClient.updateConfidence(props.knowledgeUnit.id!, request);
    
    // Show success message
    console.log('Confidence level updated successfully');
    
    isEditingConfidence.value = false;
    hasUnsavedChanges.value = false;
    
    // Emit the updated knowledge unit so parent components can refresh
    emit('updated', updatedKnowledgeUnit);
    
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
  if (!props.knowledgeUnit) return;
  
  bootbox.confirm({
    title: 'Delete Knowledge Unit',
    message: `Are you sure you want to delete this knowledge unit?<br><br>` +
             `<strong>Note:</strong> Deleted knowledge units will be hidden from all views and excluded from AI searches.<br><br>` +
             `This knowledge unit contains ${props.knowledgeUnit.fragmentCount} fragment(s). ` +
             `The fragments will remain linked but the knowledge unit will be hidden.<br><br>` +
             `You cannot delete a knowledge unit that is referenced by articles.`,
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
        await deleteKnowledgeUnit();
      }
    }
  });
}

async function deleteKnowledgeUnit(): Promise<void> {
  if (!props.knowledgeUnit) return;
  
  isDeleting.value = true;
  
  try {
    const result = await knowledgeUnitsClient.deleteKnowledgeUnit(props.knowledgeUnit.id!);
    
    if (!result.success) {
      bootbox.alert({
        title: 'Cannot Delete Knowledge Unit',
        message: result.message || 'Failed to delete knowledge unit. Please try again.',
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
    
    // Success - emit event to parent and show toast
    showToast('success', 'Knowledge unit deleted successfully');
    emit('deleted', props.knowledgeUnit.id!);
    
  } catch (error: any) {
    console.error('Error deleting knowledge unit:', error);
    
    // Check if error has a response with a message
    let errorMessage = 'Failed to delete knowledge unit. Please try again.';
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

const loadFragments = async (): Promise<void> => {
  if (!props.knowledgeUnit) return;

  loadingFragments.value = true;
  fragmentsError.value = null;
  try {
    fragments.value = await fragmentsClient.getByKnowledgeUnitId(props.knowledgeUnit.id!);
  } catch (err: any) {
    fragmentsError.value = 'Failed to load fragments: ' + err.message;
    console.error('Error loading fragments:', err);
  } finally {
    loadingFragments.value = false;
  }
};

const selectFragment = (fragment: FragmentDto): void => {
  selectedFragment.value = fragment;
};

const closeFragmentModal = (): void => {
  selectedFragment.value = null;
};

const handleFragmentUpdated = (updatedFragment: FragmentDto): void => {
  // Update the fragment in the local fragments array
  const index = fragments.value.findIndex(f => f.id === updatedFragment.id);
  if (index !== -1) {
    fragments.value[index] = updatedFragment;
  }
  // Update the selected fragment to show the new data
  selectedFragment.value = updatedFragment;
};

const handleFragmentDeleted = (fragmentId: string): void => {
  // Remove the deleted fragment from the list
  fragments.value = fragments.value.filter(f => f.id !== fragmentId);
  
  // Update fragment count for the current knowledge unit
  if (props.knowledgeUnit) {
    props.knowledgeUnit.fragmentCount = Math.max(0, props.knowledgeUnit.fragmentCount - 1);
  }
  
  // Close the modal
  selectedFragment.value = null;
};

// Expose method for parent to check unsaved changes
defineExpose({
  hasUnsavedChanges: () => hasUnsavedChanges.value
});
</script>

<style scoped>
/* Tab content container - takes remaining space and scrolls */
.tab-content {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  min-height: 0;
}

/* Each tab pane should fill available space and scroll independently */
.tab-pane {
  height: 100%;
  overflow-y: auto;
  overflow-x: hidden;
  padding: 1.5rem 0 2rem 0;
  min-height: 0;
}
</style>
