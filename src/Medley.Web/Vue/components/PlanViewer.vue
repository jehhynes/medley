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
          <div class="tiptap-dropdown active position-relative" ref="planDropdown">
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
              style="min-width: 400px;"
              @click.stop>
              <div 
                v-for="p in allPlans" 
                :key="p.id" 
                class="tiptap-dropdown-item"
                :class="{ 'is-active': p.id === plan.id }"
                @click="selectVersion(p.id)">
                <div class="d-flex justify-content-between align-items-center gap-3 w-100">
                  <strong>v{{ p.version }}</strong>
                  <span class="text-muted small">{{ p.status === 'Draft' ? '(current)' : '' }}</span>
                  <span class="text-muted small">{{ formatDate(p.createdAt) }}</span>
                  <span class="text-muted small">{{ p.createdBy.name }}</span>
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

<script>
import FragmentModal from './FragmentModal.vue';
import { 
  getFragmentCategoryIcon, 
  getIconClass, 
  getConfidenceIcon, 
  getConfidenceColor,
  getArticleTypes 
} from '@/utils/helpers.js';

export default {
  name: 'PlanViewer',
  components: {
    FragmentModal
  },
  props: {
    planId: {
      type: String,
      required: true
    },
    articleId: {
      type: String,
      required: true
    }
  },
  emits: ['conversation-created', 'close-plan'],
  data() {
    return {
      plan: null,
      allPlans: [],
      loading: true,
      error: null,
      isSaving: false,
      planInstructions: '',
      savingFragments: new Set(),
      selectedFragment: null,
      isRestoringPlan: false,
      versionDropdownOpen: false
    };
  },
  computed: {
    includedFragments() {
      if (!this.plan) return [];
      return this.plan.fragments.filter(f => f.include);
    },
    excludedFragments() {
      if (!this.plan) return [];
      return this.plan.fragments.filter(f => !f.include);
    },
    isArchivedPlan() {
      return this.plan && this.plan.status !== 'Draft';
    },
    canEditPlan() {
      return this.plan && this.plan.status === 'Draft';
    }
  },
  watch: {
    planId: {
      immediate: true,
      handler() {
        this.loadPlan();
      }
    },
    'plan.instructions': {
      handler(newVal) {
        if (newVal !== undefined) {
          this.planInstructions = newVal;
        }
      },
      immediate: true
    },
    'plan.fragments': {
      handler() {
        // Auto-expand textareas when fragments change
        this.$nextTick(() => {
          const textareas = this.$el?.querySelectorAll('textarea');
          textareas?.forEach(textarea => {
            textarea.style.height = 'auto';
            textarea.style.height = textarea.scrollHeight + 'px';
          });
        });
      },
      deep: true
    }
  },
  methods: {
    async loadPlan() {
      this.loading = true;
      this.error = null;

      try {
        // Load all plans for version dropdown
        const plansResponse = await fetch(`/api/articles/${this.articleId}/plans`);
        if (plansResponse.ok) {
          this.allPlans = await plansResponse.json();
        }

        // Load active plan
        const response = await fetch(`/api/articles/${this.articleId}/plans/active`);
        
        if (response.status === 204) {
          this.error = 'No active plan found';
          return;
        }

        if (!response.ok) {
          throw new Error('Failed to load plan');
        }

        this.plan = await response.json();
        this.planInstructions = this.plan.instructions || '';
        
        // Auto-expand textareas after plan loads
        this.$nextTick(() => {
          const textareas = this.$el?.querySelectorAll('textarea');
          textareas?.forEach(textarea => {
            textarea.style.height = 'auto';
            textarea.style.height = textarea.scrollHeight + 'px';
          });
        });
      } catch (err) {
        console.error('Error loading plan:', err);
        this.error = err.message;
      } finally {
        this.loading = false;
      }
    },

    async loadSpecificPlan(planId) {
      this.loading = true;
      this.error = null;

      try {
        const response = await fetch(`/api/articles/${this.articleId}/plans/${planId}`);
        
        if (!response.ok) {
          throw new Error('Failed to load plan');
        }

        this.plan = await response.json();
        this.planInstructions = this.plan.instructions || '';
        
        // Auto-expand textareas after plan loads
        this.$nextTick(() => {
          const textareas = this.$el?.querySelectorAll('textarea');
          textareas?.forEach(textarea => {
            textarea.style.height = 'auto';
            textarea.style.height = textarea.scrollHeight + 'px';
          });
        });
      } catch (err) {
        console.error('Error loading plan:', err);
        this.error = err.message;
      } finally {
        this.loading = false;
      }
    },

    toggleVersionDropdown() {
      this.versionDropdownOpen = !this.versionDropdownOpen;
    },

    async selectVersion(planId) {
      if (planId) {
        this.versionDropdownOpen = false;
        await this.loadSpecificPlan(planId);
      }
    },

    handleClickOutside(event) {
      if (this.$refs.planDropdown && !this.$refs.planDropdown.contains(event.target)) {
        this.versionDropdownOpen = false;
      }
    },

    async restorePlan() {
      if (!this.plan || this.isRestoringPlan) return;

      const confirmMessage = `Restore version ${this.plan.version} as the active plan? The current draft will be archived.`;
      
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
        callback: async (result) => {
          if (result) {
            this.isRestoringPlan = true;
            try {
              const response = await fetch(`/api/articles/${this.articleId}/plans/${this.plan.id}/restore`, {
                method: 'POST'
              });

              if (!response.ok) {
                throw new Error('Failed to restore plan');
              }

              // Reload all plans and show the restored one
              await this.loadPlan();
            } catch (err) {
              console.error('Error restoring plan:', err);
              this.error = 'Failed to restore plan: ' + err.message;
            } finally {
              this.isRestoringPlan = false;
            }
          }
        }
      });
    },

    async savePlan() {
      if (!this.plan || this.isSaving) return;

      this.isSaving = true;
      try {
        const response = await fetch(`/api/articles/${this.articleId}/plans/${this.plan.id}`, {
          method: 'PUT',
          headers: {
            'Content-Type': 'application/json'
          },
          body: JSON.stringify({
            instructions: this.planInstructions
          })
        });

        if (!response.ok) {
          throw new Error('Failed to save plan');
        }

        // Update the local plan object
        this.plan.instructions = this.planInstructions;
      } catch (err) {
        console.error('Error saving plan:', err);
        this.error = 'Failed to save plan: ' + err.message;
      } finally {
        this.isSaving = false;
      }
    },

    async acceptPlan() {
      if (!this.plan || !this.canEditPlan) return;

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
        callback: async (result) => {
          if (result) {
            this.isSaving = true;
            try {
              const response = await fetch(`/api/articles/${this.articleId}/plans/${this.plan.id}/accept`, {
                method: 'POST'
              });

              if (!response.ok) {
                const error = await response.json();
                throw new Error(error.error || 'Failed to accept plan');
              }

              const data = await response.json();
              
              // Emit event to load the new conversation in the chat panel
              if (data.conversationId) {
                this.$emit('conversation-created', data.conversationId);
              }
            } catch (err) {
              console.error('Error accepting plan:', err);
              this.error = 'Failed to accept plan: ' + err.message;
            } finally {
              this.isSaving = false;
            }
          }
        }
      });
    },

    async rejectPlan() {
      if (!this.plan || !this.canEditPlan) return;

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
        callback: async (result) => {
          if (result) {
            this.isSaving = true;
            try {
              const response = await fetch(`/api/articles/${this.articleId}/plans/${this.plan.id}/reject`, {
                method: 'POST'
              });

              if (!response.ok) {
                const error = await response.json();
                throw new Error(error.error || 'Failed to reject plan');
              }

              // Close the plan tab after successful rejection
              this.$emit('close-plan');
            } catch (err) {
              console.error('Error rejecting plan:', err);
              this.error = 'Failed to reject plan: ' + err.message;
            } finally {
              this.isSaving = false;
            }
          }
        }
      });
    },

    selectFragment(planFragment) {
      this.selectedFragment = planFragment.fragment;
    },

    closeFragmentModal() {
      this.selectedFragment = null;
    },

    async updateFragmentInclude(planFragment) {
      if (this.savingFragments.has(planFragment.fragmentId)) return;

      this.savingFragments.add(planFragment.fragmentId);
      try {
        const response = await fetch(`/api/articles/${this.articleId}/plans/${this.plan.id}/fragments/${planFragment.fragmentId}/include`, {
          method: 'PUT',
          headers: {
            'Content-Type': 'application/json'
          },
          body: JSON.stringify({
            include: planFragment.include
          })
        });

        if (!response.ok) {
          throw new Error('Failed to update fragment');
        }
      } catch (err) {
        console.error('Error updating fragment:', err);
        this.error = 'Failed to update fragment: ' + err.message;
        // Revert the change on error
        planFragment.include = !planFragment.include;
      } finally {
        this.savingFragments.delete(planFragment.fragmentId);
      }
    },

    toggleFragmentInclude(planFragment) {
      planFragment.include = !planFragment.include;
      this.updateFragmentInclude(planFragment);
    },

    async updateFragmentInstructions(planFragment) {
      if (this.savingFragments.has(planFragment.fragmentId)) return;

      this.savingFragments.add(planFragment.fragmentId);
      try {
        const response = await fetch(`/api/articles/${this.articleId}/plans/${this.plan.id}/fragments/${planFragment.fragmentId}/instructions`, {
          method: 'PUT',
          headers: {
            'Content-Type': 'application/json'
          },
          body: JSON.stringify({
            instructions: planFragment.instructions
          })
        });

        if (!response.ok) {
          throw new Error('Failed to update instructions');
        }
      } catch (err) {
        console.error('Error updating instructions:', err);
        this.error = 'Failed to update instructions: ' + err.message;
      } finally {
        this.savingFragments.delete(planFragment.fragmentId);
      }
    },

    formatDate(dateString) {
      if (!dateString) return '';
      const date = new Date(dateString);
      return date.toLocaleDateString() + ' ' + date.toLocaleTimeString();
    },

    renderMarkdown(content) {
      if (!content) return '';
      if (typeof marked !== 'undefined') {
        return marked.parse(content);
      }
      return content.replace(/\n/g, '<br>');
    },

    getSimilarityPercent(score) {
      return Math.round(score * 100);
    },

    getSimilarityClass(score) {
      if (score >= 0.8) return 'bg-success';
      if (score >= 0.6) return 'bg-warning';
      return 'bg-secondary';
    },

    getFragmentCategoryIcon,

    getIconClass,

    getConfidenceIcon,

    getConfidenceColor,

    handleKeydown(event) {
      // Fragment modal handles its own Escape key
    },

    autoExpandTextarea(event) {
      const textarea = event.target;
      textarea.style.height = 'auto';
      textarea.style.height = textarea.scrollHeight + 'px';
    }
  },

  mounted() {
    // Preload article types for icon display
    getArticleTypes();
    
    // Auto-expand all textareas on mount
    this.$nextTick(() => {
      const textareas = this.$el.querySelectorAll('textarea');
      textareas.forEach(textarea => {
        textarea.style.height = 'auto';
        textarea.style.height = textarea.scrollHeight + 'px';
      });
    });

    // Add click-outside listener for dropdown
    document.addEventListener('click', this.handleClickOutside);
  },

  beforeUnmount() {
    // Remove click-outside listener
    document.removeEventListener('click', this.handleClickOutside);
  }
};
</script>

