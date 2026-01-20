<template>
  <div class="plan-viewer">
    <div v-if="loading" class="loading-spinner">
      <div class="spinner-border" role="status">
        <span class="visually-hidden">Loading plan...</span>
      </div>
    </div>

    <div v-else-if="error" class="alert alert-danger m-3">
      {{ error }}
    </div>

    <template v-else-if="plan">
      <!-- Instructions Editor (serves as page header) -->
      <tiptap-editor
        v-model="planInstructions"
        :is-saving="isSaving"
        :auto-save="true"
        :show-save-button="false"
        :show-formatting="false"
        :readonly="!canEditPlan"
        @save="savePlan"
      >
        <template #toolbar-prepend>
          <button 
            type="button"
            class="tiptap-toolbar-btn tiptap-btn-with-text is-active"
            @click="acceptPlan"
            :disabled="!canEditPlan"
            title="Accept Plan">
            <i class="bi bi-check-lg me-1"></i>
            Accept
          </button>
          <button 
            type="button"
            class="tiptap-toolbar-btn tiptap-btn-with-text"
            @click="rejectPlan"
            :disabled="!canEditPlan"
            title="Reject Plan">
            <i class="bi bi-x-lg me-1"></i>
            Reject
          </button>
          <div class="tiptap-toolbar-divider"></div>
        </template>
        <template #toolbar-append>
          <!-- Restore Button (for archived plans) -->
          <button 
            v-if="isArchivedPlan"
            type="button"
            class="btn btn-sm btn-primary me-2"
            @click="restorePlan"
            :disabled="isRestoringPlan"
            title="Restore this version as active">
            <span v-if="isRestoringPlan" class="spinner-border spinner-border-sm me-1" role="status"></span>
            <i v-else class="bi bi-arrow-counterclockwise me-1"></i>
            Restore
          </button>

          <div class="tiptap-toolbar-divider"></div>
          
          <!-- Version Dropdown -->
          <div class="tiptap-dropdown active position-relative" ref="planDropdown" style="margin-left: auto;">
            <button 
              type="button"
              class="tiptap-toolbar-btn tiptap-btn-with-text"
              @click="toggleVersionDropdown"
              title="Select version">
              v{{ plan.version }}
              <i class="bi bi-chevron-down ms-1"></i>
            </button>
            <div 
              v-if="versionDropdownOpen" 
              class="tiptap-dropdown-menu right-aligned"
              style="min-width: 400px; max-height: 400px; overflow-y: auto;"
              @click.stop>
              <div 
                v-for="p in allPlans" 
                :key="p.id" 
                class="tiptap-dropdown-item"
                :class="{ 'is-active': p.id === plan.id }"
                @click="selectVersion(p.id)">
                <div class="d-flex justify-content-between align-items-center gap-3 w-100">
                  <strong>v{{ p.version }}</strong>
                  <span class="text-muted small">{{ formatDate(p.createdAt) }}</span>
                  <span class="text-muted small">{{ p.createdBy.fullName }}</span>
                </div>
              </div>
            </div>
          </div>
        </template>
        <template #notifications>
          <!-- Changes Summary (for modified plans) -->
          <div v-if="plan.changesSummary" class="alert alert-info mx-3 mt-3 mb-0">
            <strong><i class="bi bi-info-circle me-2"></i>Changes from v{{ plan.version - 1 }}:</strong>
            <div class="mt-2">{{ plan.changesSummary }}</div>
          </div>

          <!-- Archived Plan Warning -->
          <div v-if="isArchivedPlan" class="alert alert-warning mx-3 mt-3 mb-0">
            <i class="bi bi-exclamation-triangle me-2"></i>
            You are viewing an archived version of this plan. Use the "Restore" button to make it active.
          </div>
        </template>
      </tiptap-editor>

      <!-- Fragments Section -->
      <div class="plan-fragments-container">
        <!-- Included Fragments -->
        <div>
          <div class="d-flex justify-content-between align-items-center p-2 " style="background-color: var(--bs-border-color);">
            <h5 class="mb-0 text-center flex-grow-1">Included Fragments</h5>
            <span class="badge bg-success">{{ includedFragments.length }}</span>
          </div>
          <div v-if="includedFragments.length === 0" class="text-muted text-center py-3 border rounded">
            No fragments included in this plan
          </div>
          <div v-else>
            <div 
              v-for="pf in includedFragments" 
              :key="pf.fragmentId"
              class="fragment-item border-bottom py-3 px-3">
              <!-- Header Row: Icon, Title, Badges -->
              <div class="row g-2">
                <div class="col-auto" style="width: 60px; text-align: center;">
                  <i class="list-item-icon" :class="getIconClass(getFragmentCategoryIcon(pf.fragment.category))" style="font-size: 1.5rem;position:relative;top:-0.33rem;"></i>
                </div>
                <div class="col">
                  <div class="fw-semibold fragment-title" @click="selectFragment(pf)">{{ pf.fragment.title || 'Untitled Fragment' }}</div>
                </div>
                <div class="col-auto">
                  <span class="badge me-2" :class="getSimilarityClass(pf.similarityScore)">
                    {{ getSimilarityPercent(pf.similarityScore) }}%
                  </span>
                  <span v-if="pf.fragment.confidence !== null && pf.fragment.confidence !== undefined">
                    <i 
                      :class="'fa-duotone ' + getConfidenceIcon(pf.fragment.confidence)" 
                      :style="{ color: getConfidenceColor(pf.fragment.confidence) }"
                      :title="'Confidence: ' + (pf.fragment.confidence || '')"
                      style="font-size: 1.25rem;"
                    ></i>
                  </span>
                </div>
              </div>
              
              <!-- Content Row: Button, Summary/Instructions -->
              <div class="row g-2 gy-0">
                <!-- Button Column -->
                <div class="col-auto pt-1" style="width: 60px; text-align: center;">
                  <button 
                    class="btn btn-sm btn-danger"
                    @click.stop="toggleFragmentInclude(pf)"
                    :disabled="savingFragments.has(pf.fragmentId) || !canEditPlan"
                    title="Exclude this fragment"
                    style="padding: 0.25rem 0.5rem;">
                    <span v-if="savingFragments.has(pf.fragmentId)" class="spinner-border spinner-border-sm" role="status"></span>
                    <i v-else class="bi bi-arrow-down"></i>
                  </button>
                </div>
                
                <!-- Content Column -->
                <div class="col">
                  <div class="row gy-0">
                    <!-- Summary Column -->
                    <div class="col-12 col-xxl-6">
                      <div v-if="pf.fragment.summary" class="text-muted small">{{ pf.fragment.summary }}</div>
                    </div>
                    
                    <!-- Instructions Column (moves to right on xxl+) -->
                    <div class="col-12 col-xxl-6" @click.stop>
                      <textarea 
                        class="form-control form-control-sm"
                        v-model="pf.instructions"
                        @change="updateFragmentInstructions(pf)"
                        @input="autoExpandTextarea($event)"
                        :disabled="!canEditPlan"
                        placeholder="How to use this fragment..."
                        style="min-height: 100%; resize: vertical; overflow: hidden;"></textarea>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Excluded Fragments -->
        <div>
          <div class="d-flex justify-content-between align-items-center p-2 " style="background-color: var(--bs-border-color);">
            <h5 class="mb-0 text-center flex-grow-1">Excluded Fragments</h5>
            <span class="badge bg-danger">{{ excludedFragments.length }}</span>
          </div>
          <div v-if="excludedFragments.length === 0" class="text-muted text-center py-3 border rounded">
            No fragments excluded from this plan
          </div>
          <div v-else>
            <div 
              v-for="pf in excludedFragments" 
              :key="pf.fragmentId"
              class="fragment-item border-bottom py-3 px-3">
              <!-- Header Row: Icon, Title, Badges -->
              <div class="row g-2">
                <div class="col-auto" style="width: 60px; text-align: center;">
                  <i class="list-item-icon" :class="getIconClass(getFragmentCategoryIcon(pf.fragment.category))" style="font-size: 1.5rem;position:relative;top:-0.33rem;"></i>
                </div>
                <div class="col">
                  <div class="fw-semibold fragment-title" @click="selectFragment(pf)">{{ pf.fragment.title || 'Untitled Fragment' }}</div>
                </div>
                <div class="col-auto">
                  <span class="badge me-2" :class="getSimilarityClass(pf.similarityScore)">
                    {{ getSimilarityPercent(pf.similarityScore) }}%
                  </span>
                  <span v-if="pf.fragment.confidence !== null && pf.fragment.confidence !== undefined">
                    <i 
                      :class="'fa-duotone ' + getConfidenceIcon(pf.fragment.confidence)" 
                      :style="{ color: getConfidenceColor(pf.fragment.confidence) }"
                      :title="'Confidence: ' + (pf.fragment.confidence || '')"
                      style="font-size: 1.25rem;"
                    ></i>
                  </span>
                </div>
              </div>
              
              <!-- Content Row: Button, Summary/Reasoning -->
              <div class="row g-2 gy-0">
                <!-- Button Column -->
                <div class="col-auto pt-1" style="width: 60px; text-align: center;">
                  <button 
                    class="btn btn-sm btn-success"
                    @click.stop="toggleFragmentInclude(pf)"
                    :disabled="savingFragments.has(pf.fragmentId) || !canEditPlan"
                    title="Include this fragment"
                    style="padding: 0.25rem 0.5rem;">
                    <span v-if="savingFragments.has(pf.fragmentId)" class="spinner-border spinner-border-sm" role="status"></span>
                    <i v-else class="bi bi-arrow-up"></i>
                  </button>
                </div>
                
                <!-- Content Column -->
                <div class="col">
                  <div class="row gy-0">
                    <!-- Summary Column -->
                    <div class="col-12 col-xxl-6">
                      <div v-if="pf.fragment.summary" class="text-muted small">{{ pf.fragment.summary }}</div>
                    </div>
                    
                    <!-- Reasoning Column (moves to right on xxl+) -->
                    <div class="col-12 col-xxl-6">
                      <div class="fst-italic text-muted small">{{ pf.reasoning }}</div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </template>

    <!-- Fragment Detail Modal -->
    <fragment-modal
      :fragment="selectedFragment"
      :visible="!!selectedFragment"
      @close="closeFragmentModal"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted, onBeforeUnmount, nextTick } from 'vue';
import FragmentModal from '../../sources/components/FragmentModal.vue';
import { 
  getFragmentCategoryIcon, 
  getIconClass, 
  getConfidenceIcon, 
  getConfidenceColor,
  getArticleTypes 
} from '@/utils/helpers';
import { apiClients } from '@/utils/apiClients';
import type { FragmentDto, UserRef } from '@/types/api-client';

// Declare global types
declare const bootbox: {
  confirm: (options: {
    title: string;
    message: string;
    buttons: {
      confirm: { label: string; className: string };
      cancel: { label: string; className: string };
    };
    callback: (result: boolean) => void;
  }) => void;
};

declare const marked: {
  parse: (markdown: string) => string;
};

// Plan types (not yet in generated types)
interface PlanFragmentDto {
  fragmentId: string;
  fragment: FragmentDto;
  include: boolean;
  similarityScore: number;
  reasoning: string;
  instructions: string;
}

interface PlanDto {
  id: string;
  articleId: string;
  version: number;
  instructions: string;
  fragments: PlanFragmentDto[];
  status: string;
  createdBy: UserRef;
  createdAt: Date;
  changesSummary?: string;
}

// Props
interface Props {
  planId: string;
  articleId: string;
}

const props = defineProps<Props>();

// Emits
interface Emits {
  (e: 'conversation-created', conversationId: string): void;
  (e: 'close-plan'): void;
}

const emit = defineEmits<Emits>();

// State
const plan = ref<PlanDto | null>(null);
const allPlans = ref<PlanDto[]>([]);
const loading = ref<boolean>(true);
const error = ref<string | null>(null);
const isSaving = ref<boolean>(false);
const planInstructions = ref<string>('');
const savingFragments = ref<Set<string>>(new Set());
const selectedFragment = ref<FragmentDto | null>(null);
const isRestoringPlan = ref<boolean>(false);
const versionDropdownOpen = ref<boolean>(false);
const planDropdown = ref<HTMLElement | null>(null);

// Computed
const includedFragments = computed<PlanFragmentDto[]>(() => {
  if (!plan.value) return [];
  return plan.value.fragments.filter(f => f.include);
});

const excludedFragments = computed<PlanFragmentDto[]>(() => {
  if (!plan.value) return [];
  return plan.value.fragments.filter(f => !f.include);
});

const isArchivedPlan = computed<boolean>(() => {
  return plan.value !== null && plan.value.status !== 'Draft';
});

const canEditPlan = computed<boolean>(() => {
  return plan.value !== null && plan.value.status === 'Draft';
});

// Watch
watch(() => props.planId, () => {
  loadPlan();
}, { immediate: true });

watch(() => plan.value?.instructions, (newVal) => {
  if (newVal !== undefined) {
    planInstructions.value = newVal;
  }
}, { immediate: true });

watch(() => plan.value?.fragments, () => {
  // Auto-expand textareas when fragments change
  nextTick(() => {
    const textareas = document.querySelectorAll<HTMLTextAreaElement>('textarea');
    textareas.forEach(textarea => {
      textarea.style.height = 'auto';
      textarea.style.height = textarea.scrollHeight + 'px';
    });
  });
}, { deep: true });

// Methods
async function loadPlan(): Promise<void> {
  loading.value = true;
  error.value = null;

  try {
    // Load all plans for version dropdown
    allPlans.value = await apiClients.plans.getAllPlans(props.articleId);

    // Load active plan
    try {
      plan.value = await apiClients.plans.getActivePlan(props.articleId);
      planInstructions.value = plan.value.instructions || '';
    } catch (err: any) {
      error.value = 'No active plan found';
      return;
    }
    
    // Auto-expand textareas after plan loads
    nextTick(() => {
      const textareas = document.querySelectorAll<HTMLTextAreaElement>('textarea');
      textareas.forEach(textarea => {
        textarea.style.height = 'auto';
        textarea.style.height = textarea.scrollHeight + 'px';
      });
    });
  } catch (err: any) {
    console.error('Error loading plan:', err);
    error.value = err.message;
  } finally {
    loading.value = false;
  }
}

async function loadSpecificPlan(planId: string): Promise<void> {
  loading.value = true;
  error.value = null;

  try {
    plan.value = await apiClients.plans.getPlan(props.articleId, planId);
    planInstructions.value = plan.value.instructions || '';
    
    // Auto-expand textareas after plan loads
    nextTick(() => {
      const textareas = document.querySelectorAll<HTMLTextAreaElement>('textarea');
      textareas.forEach(textarea => {
        textarea.style.height = 'auto';
        textarea.style.height = textarea.scrollHeight + 'px';
      });
    });
  } catch (err: any) {
    console.error('Error loading plan:', err);
    error.value = err.message;
  } finally {
    loading.value = false;
  }
}

function toggleVersionDropdown(): void {
  versionDropdownOpen.value = !versionDropdownOpen.value;
}

async function selectVersion(planId: string): Promise<void> {
  if (planId) {
    versionDropdownOpen.value = false;
    await loadSpecificPlan(planId);
  }
}

function handleClickOutside(event: MouseEvent): void {
  if (planDropdown.value && !planDropdown.value.contains(event.target as Node)) {
    versionDropdownOpen.value = false;
  }
}

async function restorePlan(): Promise<void> {
  if (!plan.value || isRestoringPlan.value) return;

  const confirmMessage = `Restore version ${plan.value.version} as the active plan? The current draft will be archived.`;
  
  bootbox.confirm({
    title: 'Restore Plan',
    message: confirmMessage,
    buttons: {
      confirm: {
        label: 'Restore',
        className: 'btn-primary'
      },
      cancel: {
        label: 'Cancel',
        className: 'btn-secondary'
      }
    },
    callback: async (result: boolean) => {
      if (result) {
        isRestoringPlan.value = true;
        try {
          await apiClients.plans.restorePlan(props.articleId, plan.value!.id);

          // Reload all plans and show the restored one
          await loadPlan();
        } catch (err: any) {
          console.error('Error restoring plan:', err);
          error.value = 'Failed to restore plan: ' + err.message;
        } finally {
          isRestoringPlan.value = false;
        }
      }
    }
  });
}

async function savePlan(): Promise<void> {
  if (!plan.value || isSaving.value) return;

  isSaving.value = true;
  try {
    await apiClients.plans.updatePlan(props.articleId, plan.value.id, {
      instructions: planInstructions.value
    });

    // Update the local plan object
    plan.value.instructions = planInstructions.value;
  } catch (err: any) {
    console.error('Error saving plan:', err);
    error.value = 'Failed to save plan: ' + err.message;
  } finally {
    isSaving.value = false;
  }
}

async function acceptPlan(): Promise<void> {
  if (!plan.value || !canEditPlan.value) return;

  bootbox.confirm({
    title: 'Accept Plan',
    message: "Accept this plan and begin AI implementation?",
    buttons: {
      confirm: {
        label: 'Accept',
        className: 'btn-success'
      },
      cancel: {
        label: 'Cancel',
        className: 'btn-secondary'
      }
    },
    callback: async (result: boolean) => {
      if (result) {
        isSaving.value = true;
        try {
          const data = await apiClients.plans.acceptPlan(props.articleId, plan.value!.id);
          
          // Emit event to load the new conversation in the chat panel
          if (data.conversationId) {
            emit('conversation-created', data.conversationId);
          }
        } catch (err: any) {
          console.error('Error accepting plan:', err);
          error.value = 'Failed to accept plan: ' + err.message;
        } finally {
          isSaving.value = false;
        }
      }
    }
  });
}

async function rejectPlan(): Promise<void> {
  if (!plan.value || !canEditPlan.value) return;

  bootbox.confirm({
    title: 'Reject Plan',
    message: "Reject this plan? It will be archived.",
    buttons: {
      confirm: {
        label: 'Reject',
        className: 'btn-danger'
      },
      cancel: {
        label: 'Cancel',
        className: 'btn-secondary'
      }
    },
    callback: async (result: boolean) => {
      if (result) {
        isSaving.value = true;
        try {
          await apiClients.plans.rejectPlan(props.articleId, plan.value!.id);

          // Close the plan tab after successful rejection
          emit('close-plan');
        } catch (err: any) {
          console.error('Error rejecting plan:', err);
          error.value = 'Failed to reject plan: ' + err.message;
        } finally {
          isSaving.value = false;
        }
      }
    }
  });
}

function selectFragment(planFragment: PlanFragmentDto): void {
  selectedFragment.value = planFragment.fragment;
}

function closeFragmentModal(): void {
  selectedFragment.value = null;
}

async function updateFragmentInclude(planFragment: PlanFragmentDto): Promise<void> {
  if (savingFragments.value.has(planFragment.fragmentId)) return;

  savingFragments.value.add(planFragment.fragmentId);
  try {
    await apiClients.plans.updatePlanFragmentInclude(
      props.articleId,
      plan.value!.id,
      planFragment.fragmentId,
      { include: planFragment.include }
    );
  } catch (err: any) {
    console.error('Error updating fragment:', err);
    error.value = 'Failed to update fragment: ' + err.message;
    // Revert the change on error
    planFragment.include = !planFragment.include;
  } finally {
    savingFragments.value.delete(planFragment.fragmentId);
  }
}

function toggleFragmentInclude(planFragment: PlanFragmentDto): void {
  planFragment.include = !planFragment.include;
  updateFragmentInclude(planFragment);
}

async function updateFragmentInstructions(planFragment: PlanFragmentDto): Promise<void> {
  if (savingFragments.value.has(planFragment.fragmentId)) return;

  savingFragments.value.add(planFragment.fragmentId);
  try {
    await apiClients.plans.updatePlanFragmentInstructions(
      props.articleId,
      plan.value!.id,
      planFragment.fragmentId,
      { instructions: planFragment.instructions }
    );
  } catch (err: any) {
    console.error('Error updating instructions:', err);
    error.value = 'Failed to update instructions: ' + err.message;
  } finally {
    savingFragments.value.delete(planFragment.fragmentId);
  }
}

function formatDate(dateString: Date | undefined): string {
  if (!dateString) return '';
  const date = new Date(dateString);
  return date.toLocaleDateString() + ' ' + date.toLocaleTimeString();
}

function renderMarkdown(content: string): string {
  if (!content) return '';
  if (typeof marked !== 'undefined') {
    return marked.parse(content);
  }
  return content.replace(/\n/g, '<br>');
}

function getSimilarityPercent(score: number): number {
  return Math.round(score * 100);
}

function getSimilarityClass(score: number): string {
  if (score >= 0.8) return 'bg-success';
  if (score >= 0.6) return 'bg-warning';
  return 'bg-secondary';
}

function autoExpandTextarea(event: Event): void {
  const textarea = event.target as HTMLTextAreaElement;
  textarea.style.height = 'auto';
  textarea.style.height = textarea.scrollHeight + 'px';
}

// Lifecycle
onMounted(() => {
  // Preload article types for icon display
  getArticleTypes();
  
  // Auto-expand all textareas on mount
  nextTick(() => {
    const textareas = document.querySelectorAll<HTMLTextAreaElement>('textarea');
    textareas.forEach(textarea => {
      textarea.style.height = 'auto';
      textarea.style.height = textarea.scrollHeight + 'px';
    });
  });

  // Add click-outside listener for dropdown
  document.addEventListener('click', handleClickOutside);
});

onBeforeUnmount(() => {
  // Remove click-outside listener
  document.removeEventListener('click', handleClickOutside);
});
</script>

