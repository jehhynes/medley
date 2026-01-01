// Plan Viewer Component - Display article improvement plans
const PlanViewer = {
    name: 'PlanViewer',
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
            expandedFragments: new Set()
        };
    },
    computed: {
        includeFragments() {
            if (!this.plan) return [];
            return this.plan.fragments.filter(f => f.include);
        },
        referenceFragments() {
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
        }
    },
    methods: {
        async loadPlan() {
            this.loading = true;
            this.error = null;

            try {
                const response = await fetch(`/api/articles/${this.articleId}/assistant/plans/active`);
                
                if (response.status === 204) {
                    this.error = 'No active plan found';
                    return;
                }

                if (!response.ok) {
                    throw new Error('Failed to load plan');
                }

                this.plan = await response.json();
            } catch (err) {
                console.error('Error loading plan:', err);
                this.error = err.message;
            } finally {
                this.loading = false;
            }
        },

        toggleFragment(fragmentId) {
            if (this.expandedFragments.has(fragmentId)) {
                this.expandedFragments.delete(fragmentId);
            } else {
                this.expandedFragments.add(fragmentId);
            }
        },

        isExpanded(fragmentId) {
            return this.expandedFragments.has(fragmentId);
        },

        copyToClipboard(text) {
            navigator.clipboard.writeText(text).then(() => {
                // Show success feedback (could use a toast notification)
                console.log('Copied to clipboard');
            }).catch(err => {
                console.error('Failed to copy:', err);
            });
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
            if (score >= 0.8) return 'text-success';
            if (score >= 0.6) return 'text-warning';
            return 'text-secondary';
        }
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

            <div v-else-if="plan" class="plan-content">
                <!-- Plan Header -->
                <div class="plan-header">
                    <div class="plan-meta">
                        <span class="badge bg-primary">Plan</span>
                        <span class="text-muted ms-2">Created {{ formatDate(plan.createdAt) }}</span>
                        <span v-if="plan.createdBy" class="text-muted ms-2">by {{ plan.createdBy.name }}</span>
                    </div>
                </div>

                <!-- Instructions Section -->
                <div class="plan-section">
                    <h5 class="plan-section-title">
                        <i class="bi bi-lightbulb me-2"></i>
                        Improvement Instructions
                    </h5>
                    <div class="plan-instructions markdown-container" v-html="renderMarkdown(plan.instructions)"></div>
                </div>

                <!-- Include Fragments Section -->
                <div v-if="includeFragments.length > 0" class="plan-section">
                    <h5 class="plan-section-title">
                        <i class="bi bi-check-circle me-2"></i>
                        Fragments to Include ({{ includeFragments.length }})
                    </h5>
                    <div class="fragments-list">
                        <div 
                            v-for="pf in includeFragments" 
                            :key="pf.fragmentId" 
                            class="fragment-card">
                            <div class="fragment-card-header" @click="toggleFragment(pf.fragmentId)">
                                <div class="fragment-card-title">
                                    <i class="bi" :class="isExpanded(pf.fragmentId) ? 'bi-chevron-down' : 'bi-chevron-right'"></i>
                                    <strong>{{ pf.fragment.title || 'Untitled Fragment' }}</strong>
                                    <span class="badge bg-success ms-2">Include</span>
                                    <span class="similarity-badge ms-2" :class="getSimilarityClass(pf.similarityScore)">
                                        {{ getSimilarityPercent(pf.similarityScore) }}% match
                                    </span>
                                </div>
                                <div class="fragment-card-actions" @click.stop>
                                    <button 
                                        class="btn btn-sm btn-outline-secondary"
                                        @click="copyToClipboard(pf.fragment.content)"
                                        title="Copy to clipboard">
                                        <i class="bi bi-clipboard"></i>
                                    </button>
                                </div>
                            </div>

                            <div v-if="isExpanded(pf.fragmentId)" class="fragment-card-body">
                                <div v-if="pf.fragment.summary" class="fragment-summary mb-2">
                                    {{ pf.fragment.summary }}
                                </div>

                                <div class="fragment-metadata mb-2">
                                    <span v-if="pf.fragment.category" class="badge bg-secondary me-2">
                                        {{ pf.fragment.category }}
                                    </span>
                                    <span v-if="pf.fragment.source" class="text-muted small">
                                        <i class="bi bi-file-text me-1"></i>
                                        {{ pf.fragment.source.name }}
                                        <span v-if="pf.fragment.source.date" class="ms-1">
                                            ({{ new Date(pf.fragment.source.date).toLocaleDateString() }})
                                        </span>
                                    </span>
                                </div>

                                <div class="fragment-reasoning alert alert-info">
                                    <strong>Why include this:</strong>
                                    <p class="mb-0 mt-1">{{ pf.reasoning }}</p>
                                </div>

                                <div class="fragment-instructions alert alert-primary">
                                    <strong>How to use:</strong>
                                    <p class="mb-0 mt-1">{{ pf.instructions }}</p>
                                </div>

                                <div class="fragment-content">
                                    <strong>Content:</strong>
                                    <pre class="mt-2">{{ pf.fragment.content }}</pre>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>

                <!-- Reference Fragments Section -->
                <div v-if="referenceFragments.length > 0" class="plan-section">
                    <h5 class="plan-section-title">
                        <i class="bi bi-info-circle me-2"></i>
                        Reference Only ({{ referenceFragments.length }})
                    </h5>
                    <div class="fragments-list">
                        <div 
                            v-for="pf in referenceFragments" 
                            :key="pf.fragmentId" 
                            class="fragment-card reference-fragment">
                            <div class="fragment-card-header" @click="toggleFragment(pf.fragmentId)">
                                <div class="fragment-card-title">
                                    <i class="bi" :class="isExpanded(pf.fragmentId) ? 'bi-chevron-down' : 'bi-chevron-right'"></i>
                                    <strong>{{ pf.fragment.title || 'Untitled Fragment' }}</strong>
                                    <span class="badge bg-secondary ms-2">Reference</span>
                                    <span class="similarity-badge ms-2" :class="getSimilarityClass(pf.similarityScore)">
                                        {{ getSimilarityPercent(pf.similarityScore) }}% match
                                    </span>
                                </div>
                            </div>

                            <div v-if="isExpanded(pf.fragmentId)" class="fragment-card-body">
                                <div v-if="pf.fragment.summary" class="fragment-summary mb-2">
                                    {{ pf.fragment.summary }}
                                </div>

                                <div class="fragment-metadata mb-2">
                                    <span v-if="pf.fragment.category" class="badge bg-secondary me-2">
                                        {{ pf.fragment.category }}
                                    </span>
                                    <span v-if="pf.fragment.source" class="text-muted small">
                                        <i class="bi bi-file-text me-1"></i>
                                        {{ pf.fragment.source.name }}
                                    </span>
                                </div>

                                <div class="fragment-reasoning alert alert-secondary">
                                    <strong>Note:</strong>
                                    <p class="mb-0 mt-1">{{ pf.reasoning }}</p>
                                </div>

                                <div class="fragment-content">
                                    <strong>Content:</strong>
                                    <pre class="mt-2">{{ pf.fragment.content }}</pre>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    `
};

// Make component globally available
window.PlanViewer = PlanViewer;
