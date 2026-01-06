// Plan Viewer Component - Display article improvement plans
const PlanViewer = {
    name: 'PlanViewer',
    components: {
        'fragment-modal': FragmentModal
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
            isRestoringPlan: false
        };
    },
    computed: {
        tiptapEditorComponent() {
            return window.TiptapEditor;
        },
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

        async onVersionChange(event) {
            const planId = event.target.value;
            if (planId) {
                await this.loadSpecificPlan(planId);
            }
        },

        async restorePlan() {
            if (!this.plan || this.isRestoringPlan) return;

            const confirmMessage = `Restore version ${this.plan.version} as the active plan? The current draft will be archived.`;
            
            if (!confirm(confirmMessage)) {
                return;
            }

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

        acceptPlan() {
            // TODO: Implement accept plan logic
            console.log('Accept plan clicked');
        },

        rejectPlan() {
            // TODO: Implement reject plan logic
            console.log('Reject plan clicked');
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

        getFragmentCategoryIcon(category) {
            return window.MedleyUtils.getFragmentCategoryIcon(category);
        },

        getIconClass(icon) {
            return window.MedleyUtils.getIconClass(icon);
        },

        getConfidenceIcon(confidence) {
            return window.MedleyUtils.getConfidenceIcon(confidence);
        },

        getConfidenceColor(confidence) {
            return window.MedleyUtils.getConfidenceColor(confidence);
        },

        getConfidenceLabel(confidence) {
            return window.MedleyUtils.getConfidenceLabel(confidence);
        },

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
        window.MedleyUtils.getArticleTypes();
        
        // Auto-expand all textareas on mount
        this.$nextTick(() => {
            const textareas = this.$el.querySelectorAll('textarea');
            textareas.forEach(textarea => {
                textarea.style.height = 'auto';
                textarea.style.height = textarea.scrollHeight + 'px';
            });
        });
    },

    beforeUnmount() {
        // Fragment modal handles its own cleanup
    },
    template: `
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
                <component
                    :is="tiptapEditorComponent"
                    v-if="tiptapEditorComponent"
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
                            class="tiptap-toolbar-btn"
                            @click="acceptPlan"
                            :disabled="!canEditPlan"
                            title="Accept Plan">
                            <i class="bi bi-check-lg"></i>
                        </button>
                        <button 
                            type="button"
                            class="tiptap-toolbar-btn"
                            @click="rejectPlan"
                            :disabled="!canEditPlan"
                            title="Reject Plan">
                            <i class="bi bi-x-lg"></i>
                        </button>
                        <div class="tiptap-toolbar-divider"></div>
                    </template>
                    <template #toolbar-append>
                        <div class="tiptap-toolbar-divider"></div>
                        
                        <!-- Version Dropdown -->
                        <div class="d-flex align-items-center me-2">
                            <select 
                                class="form-select form-select-sm"
                                :value="plan.id"
                                @change="onVersionChange"
                                style="min-width: 150px;">
                                <option 
                                    v-for="p in allPlans" 
                                    :key="p.id" 
                                    :value="p.id">
                                    v{{ p.version }} {{ p.status === 'Draft' ? '(current)' : '' }} - {{ formatDate(p.createdAt) }}
                                </option>
                            </select>
                        </div>

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

                        <span class="text-muted small align-self-center">by {{ plan.createdBy.name }}</span>
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
                </component>
                <div v-else class="alert alert-info">Loading editor...</div>

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
                                class="fragment-item border-bottom py-3 px-3"
                                style="cursor: pointer;"
                                @click="selectFragment(pf)">
                                <!-- Header Row: Icon, Title, Badges -->
                                <div class="row g-2">
                                    <div class="col-auto" style="width: 60px; text-align: center;">
                                        <i class="list-item-icon" :class="getIconClass(getFragmentCategoryIcon(pf.fragment.category))" style="font-size: 1.5rem;position:relative;top:-0.33rem;"></i>
                                    </div>
                                    <div class="col">
                                        <div class="fw-semibold">{{ pf.fragment.title || 'Untitled Fragment' }}</div>
                                    </div>
                                    <div class="col-auto">
                                        <span class="badge me-2" :class="getSimilarityClass(pf.similarityScore)">
                                            {{ getSimilarityPercent(pf.similarityScore) }}%
                                        </span>
                                        <span v-if="pf.fragment.confidence !== null && pf.fragment.confidence !== undefined">
                                            <i 
                                                :class="'fa-duotone ' + getConfidenceIcon(pf.fragment.confidence)" 
                                                :style="{ color: getConfidenceColor(pf.fragment.confidence) }"
                                                :title="'Confidence: ' + getConfidenceLabel(pf.fragment.confidence)"
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
                                class="fragment-item border-bottom py-3 px-3"
                                style="cursor: pointer;"
                                @click="selectFragment(pf)">
                                <!-- Header Row: Icon, Title, Badges -->
                                <div class="row g-2">
                                    <div class="col-auto" style="width: 60px; text-align: center;">
                                        <i class="list-item-icon" :class="getIconClass(getFragmentCategoryIcon(pf.fragment.category))" style="font-size: 1.5rem;position:relative;top:-0.33rem;"></i>
                                    </div>
                                    <div class="col">
                                        <div class="fw-semibold">{{ pf.fragment.title || 'Untitled Fragment' }}</div>
                                    </div>
                                    <div class="col-auto">
                                        <span class="badge me-2" :class="getSimilarityClass(pf.similarityScore)">
                                            {{ getSimilarityPercent(pf.similarityScore) }}%
                                        </span>
                                        <span v-if="pf.fragment.confidence !== null && pf.fragment.confidence !== undefined">
                                            <i 
                                                :class="'fa-duotone ' + getConfidenceIcon(pf.fragment.confidence)" 
                                                :style="{ color: getConfidenceColor(pf.fragment.confidence) }"
                                                :title="'Confidence: ' + getConfidenceLabel(pf.fragment.confidence)"
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
    `
};

// Make component globally available
window.PlanViewer = PlanViewer;
