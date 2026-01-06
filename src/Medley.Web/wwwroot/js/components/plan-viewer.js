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
            loading: true,
            error: null,
            isSaving: false,
            planInstructions: '',
            savingFragments: new Set(),
            selectedFragment: null
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
        }
    },
    methods: {
        async loadPlan() {
            this.loading = true;
            this.error = null;

            try {
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
            } catch (err) {
                console.error('Error loading plan:', err);
                this.error = err.message;
            } finally {
                this.loading = false;
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
        }
    },

    mounted() {
        // Fragment modal handles its own keyboard events
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
                    @save="savePlan"
                >
                    <template #toolbar-prepend>
                        <button 
                            type="button"
                            class="btn btn-success btn-sm me-2"
                            @click="acceptPlan"
                            title="Accept Plan">
                            <i class="bi bi-check-lg"></i>
                            Accept
                        </button>
                        <button 
                            type="button"
                            class="btn btn-danger btn-sm me-2"
                            @click="rejectPlan"
                            title="Reject Plan">
                            <i class="bi bi-x-lg"></i>
                            Reject
                        </button>
                        <div class="tiptap-toolbar-divider"></div>
                        <span class="text-muted small me-2">Created {{ formatDate(plan.createdAt) }}</span>
                        <span v-if="plan.createdBy" class="text-muted small">by {{ plan.createdBy.name }}</span>
                        <div class="tiptap-toolbar-divider"></div>
                    </template>
                </component>
                <div v-else class="alert alert-info">Loading editor...</div>

                <!-- Fragments Section -->
                <div class="plan-fragments-container">
                    <!-- Included Fragments Table -->
                    <div class="mb-4">
                        <div class="d-flex justify-content-between align-items-center mb-3 p-2 " style="background-color: var(--bs-border-color);">
                            <h5 class="mb-0 text-center flex-grow-1">Included Fragments</h5>
                            <span class="badge bg-secondary">{{ includedFragments.length }}</span>
                        </div>
                        <div v-if="includedFragments.length === 0" class="text-muted text-center py-3 border rounded">
                            No fragments included in this plan
                        </div>
                        <table v-else class="table table-hover">
                            <tbody>
                                <tr 
                                    v-for="pf in includedFragments" 
                                    :key="pf.fragmentId"
                                    class="fragment-item"
                                    style="cursor: pointer;"
                                    @click="selectFragment(pf)">
                                    <td class="align-middle">
                                        <i class="list-item-icon" :class="getIconClass(getFragmentCategoryIcon(pf.fragment.category))" style="font-size: 1.5rem;"></i>
                                    </td>
                                    <td>
                                        <div class="fw-semibold mb-1">{{ pf.fragment.title || 'Untitled Fragment' }}</div>
                                        <div v-if="pf.fragment.summary" class="text-muted small mb-1">{{ pf.fragment.summary }}</div>
                                        
                                        <!-- Instructions (editable) -->
                                        <div @click.stop>
                                            <textarea 
                                                class="form-control form-control-sm"
                                                v-model="pf.instructions"
                                                @change="updateFragmentInstructions(pf)"
                                                rows="2"
                                                placeholder="How to use this fragment..."></textarea>
                                        </div>
                                    </td>
                                    <td class="align-top text-end">
                                        <div class="d-flex flex-column align-items-end gap-2">
                                            <div>
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
                                            <button 
                                                class="btn btn-sm btn-outline-danger"
                                                @click.stop="toggleFragmentInclude(pf)"
                                                :disabled="savingFragments.has(pf.fragmentId)"
                                                title="Exclude this fragment">
                                                <span v-if="savingFragments.has(pf.fragmentId)" class="spinner-border spinner-border-sm" role="status"></span>
                                                <span v-else>Exclude</span>
                                            </button>
                                        </div>
                                    </td>
                                </tr>
                            </tbody>
                        </table>
                    </div>

                    <!-- Excluded Fragments Table -->
                    <div>
                        <div class="d-flex justify-content-between align-items-center mb-3 p-2 " style="background-color: var(--bs-border-color);">
                            <h5 class="mb-0 text-center flex-grow-1">Excluded Fragments</h5>
                            <span class="badge bg-secondary">{{ excludedFragments.length }}</span>
                        </div>
                        <div v-if="excludedFragments.length === 0" class="text-muted text-center py-3 border rounded">
                            No fragments excluded from this plan
                        </div>
                        <table v-else class="table table-hover">
                            <tbody>
                                <tr 
                                    v-for="pf in excludedFragments" 
                                    :key="pf.fragmentId"
                                    class="fragment-item"
                                    style="cursor: pointer;"
                                    @click="selectFragment(pf)">
                                    <td class="align-middle">
                                        <i class="list-item-icon" :class="getIconClass(getFragmentCategoryIcon(pf.fragment.category))" style="font-size: 1.5rem;"></i>
                                    </td>
                                    <td>
                                        <div class="fw-semibold mb-1">{{ pf.fragment.title || 'Untitled Fragment' }}</div>
                                        <div v-if="pf.fragment.summary" class="text-muted small mb-1">{{ pf.fragment.summary }}</div>
                                        
                                        <!-- Reasoning (read-only) -->
                                        <div class="fst-italic text-muted small">{{ pf.reasoning }}</div>
                                    </td>
                                    <td class="align-top text-end">
                                        <div class="d-flex flex-column align-items-end gap-2">
                                            <div>
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
                                            <button 
                                                class="btn btn-sm btn-outline-success"
                                                @click.stop="toggleFragmentInclude(pf)"
                                                :disabled="savingFragments.has(pf.fragmentId)"
                                                title="Include this fragment">
                                                <span v-if="savingFragments.has(pf.fragmentId)" class="spinner-border spinner-border-sm" role="status"></span>
                                                <span v-else>Include</span>
                                            </button>
                                        </div>
                                    </td>
                                </tr>
                            </tbody>
                        </table>
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
