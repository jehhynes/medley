// Sources page Vue app (extracted from Sources/Index.cshtml)
(function () {
    const { createApp } = Vue;
    const { api, createSignalRConnection } = window.MedleyApi;
    const {
        formatDate,
        getIconClass,
        getFragmentCategoryIcon,
        getSourceTypeIcon,
        getConfidenceIcon,
        getConfidenceColor,
        getConfidenceLabel,
        initializeMarkdownRenderer,
        showToast
    } = window.MedleyUtils;
    const {
        getUrlParam,
        setUrlParam,
        findInList,
        setupPopStateHandler
    } = window.UrlUtils;

    const app = createApp({
        components: {
            'source-list': SourceList
        },
        data() {
            return {
                sources: [],
                selectedSourceId: null,
                selectedSource: null,
                loading: false,
                error: null,
                searchQuery: '',
                signalRConnection: null,
                fragments: [],
                selectedFragment: null,
                loadingFragments: false,
                fragmentsError: null,
                markdownRenderer: null,
                articleTypes: [],
                tagging: false,
                showConfidenceComment: false,
                detachPopState: null
            };
        },
        computed: {
            parsedMetadata() {
                if (!this.selectedSource || !this.selectedSource.metadataJson) {
                    return null;
                }
                try {
                    return JSON.parse(this.selectedSource.metadataJson);
                } catch (e) {
                    console.error('Failed to parse metadata JSON:', e);
                    return null;
                }
            },
            renderedMarkdown() {
                if (!this.selectedFragment || !this.selectedFragment.content || !this.markdownRenderer) {
                    return '';
                }
                try {
                    return this.markdownRenderer.parse(this.selectedFragment.content, { breaks: true, gfm: true });
                } catch (e) {
                    console.error('Failed to render markdown:', e);
                    return this.selectedFragment.content;
                }
            },
            sortedTags() {
                if (!this.selectedSource || !this.selectedSource.tags) {
                    return [];
                }
                return [...this.selectedSource.tags].sort((a, b) => {
                    const tagTypeA = (a.tagType || '').toLowerCase();
                    const tagTypeB = (b.tagType || '').toLowerCase();
                    return tagTypeA.localeCompare(tagTypeB);
                });
            }
        },
        watch: {
            parsedMetadata(newVal) {
                this.$nextTick(() => {
                    if (this.$refs.jsonViewer && newVal) {
                        this.$refs.jsonViewer.data = newVal;
                    }
                });
            },
            selectedSource(newVal) {
                this.fragments = [];
                this.selectedFragment = null;
                this.fragmentsError = null;

                if (newVal) {
                    this.loadFragments();
                }
            }
        },
        methods: {
            async loadSources() {
                this.loading = true;
                this.error = null;
                try {
                    this.sources = await api.get('/api/sources');
                } catch (err) {
                    this.error = 'Failed to load sources: ' + err.message;
                    console.error('Error loading sources:', err);
                } finally {
                    this.loading = false;
                }
            },

            async selectSource(source, replaceState = false) {
                this.selectedSourceId = source.id;

                const currentId = getUrlParam('id');
                if (currentId !== source.id) {
                    setUrlParam('id', source.id, replaceState);
                }

                try {
                    this.selectedSource = await api.get(`/api/sources/${source.id}`);
                } catch (err) {
                    console.error('Error loading source:', err);
                    this.selectedSource = null;
                }
            },

            async onSearchInput() {
                const query = this.searchQuery.trim();
                if (query.length >= 2) {
                    try {
                        this.sources = await api.get(`/api/sources/search?query=${encodeURIComponent(query)}`);
                    } catch (err) {
                        console.error('Search error:', err);
                    }
                } else if (query.length === 0) {
                    await this.loadSources();
                }
            },

            async extractFragments() {
                if (!this.selectedSource) return;
                const sourceId = this.selectedSource.id;

                if (this.fragments.length > 0) {
                    const confirmMessage = `This source already has ${this.fragments.length} fragment(s). ` +
                        'Re-extracting will delete existing fragments. Continue?';

                    bootbox.confirm({
                        message: confirmMessage,
                        buttons: {
                            confirm: {
                                label: 'Continue',
                                className: 'btn-primary'
                            },
                            cancel: {
                                label: 'Cancel',
                                className: 'btn-secondary'
                            }
                        },
                        callback: async (result) => {
                            if (result) {
                                await this.performExtraction(sourceId);
                            }
                        }
                    });
                } else {
                    await this.performExtraction(sourceId);
                }
            },

            async performExtraction(sourceId) {
                this.selectedSource.extractionStatus = 'InProgress';
                const sourceIndex = this.sources.findIndex(s => s.id === sourceId);
                if (sourceIndex !== -1) {
                    this.sources[sourceIndex].extractionStatus = 'InProgress';
                }

                try {
                    const response = await api.post(`/api/sources/${sourceId}/extract-fragments`);

                    if (!response.success) {
                        showToast('error', response.message || 'Failed to start fragment extraction');
                        this.selectedSource.extractionStatus = 'NotStarted';
                        if (sourceIndex !== -1) {
                            this.sources[sourceIndex].extractionStatus = 'NotStarted';
                        }
                    }
                } catch (err) {
                    console.error('Fragment extraction error:', err);
                    const errorMessage = err.message || 'Failed to extract fragments. Please try again.';
                    const isClustered = errorMessage.toLowerCase().includes('clustered');

                    if (isClustered) {
                        showToast('error', 'Cannot re-extract: Some fragments have been clustered. Please uncluster them first.');
                    } else {
                        showToast('error', errorMessage);
                    }

                    this.selectedSource.extractionStatus = 'NotStarted';
                    if (sourceIndex !== -1) {
                        this.sources[sourceIndex].extractionStatus = 'NotStarted';
                    }
                }
            },

            async generateTags() {
                if (!this.selectedSource) return;
                const sourceId = this.selectedSource.id;
                this.tagging = true;
                try {
                    const result = await api.post(`/api/sources/${sourceId}/tag?force=true`);
                    if (!result.success && result.message) {
                        showToast('error', result.message);
                    } else {
                        showToast('success', result.message || 'Tags generated');
                    }
                    try {
                        const updated = await api.get(`/api/sources/${sourceId}`);
                        this.selectedSource = updated;
                    } catch (err) {
                        console.error('Failed to reload source after tagging:', err);
                    }
                } catch (err) {
                    console.error('Tag generation error:', err);
                    showToast('error', err.message || 'Failed to generate tags. Please try again.');
                } finally {
                    this.tagging = false;
                }
            },

            async loadFragments() {
                if (!this.selectedSource) return;

                this.loadingFragments = true;
                this.fragmentsError = null;
                try {
                    this.fragments = await api.get(`/api/fragments/by-source/${this.selectedSource.id}`);
                } catch (err) {
                    this.fragmentsError = 'Failed to load fragments: ' + err.message;
                    console.error('Error loading fragments:', err);
                } finally {
                    this.loadingFragments = false;
                }
            },

            selectFragment(fragment) {
                this.selectedFragment = fragment;
                this.showConfidenceComment = false;
            },

            closeFragmentModal() {
                this.selectedFragment = null;
                this.showConfidenceComment = false;
            },

            toggleConfidenceComment() {
                this.showConfidenceComment = !this.showConfidenceComment;
            },

            handleKeydown(event) {
                if (event.key === 'Escape' && this.selectedFragment) {
                    this.closeFragmentModal();
                }
            },

            async loadArticleTypes() {
                try {
                    this.articleTypes = await api.get('/api/articles/types');
                } catch (err) {
                    console.error('Error loading article types:', err);
                }
            },

            getFragmentCategoryIcon(category) {
                return getFragmentCategoryIcon(category, this.articleTypes);
            },

            getIconClass,
            getSourceTypeIcon,
            getConfidenceIcon,
            getConfidenceColor,
            getConfidenceLabel,
            formatDate
        },

        async mounted() {
            this.markdownRenderer = initializeMarkdownRenderer();

            await this.loadArticleTypes();
            await this.loadSources();

            const sourceIdFromUrl = getUrlParam('id');
            if (sourceIdFromUrl) {
                const source = findInList(this.sources, sourceIdFromUrl);
                if (source) {
                    await this.selectSource(source, true);
                } else {
                    try {
                        const loadedSource = await api.get(`/api/sources/${sourceIdFromUrl}`);
                        this.sources.unshift(loadedSource);
                        this.selectedSource = loadedSource;
                        this.selectedSourceId = sourceIdFromUrl;
                    } catch (err) {
                        console.error('Error loading source from URL:', err);
                        setUrlParam('id', null, true);
                    }
                }
            }

            this.signalRConnection = createSignalRConnection('/adminHub');

            this.signalRConnection.on('FragmentExtractionComplete', async (sourceId, fragmentCount, success) => {
                const sourceIndex = this.sources.findIndex(s => s.id === sourceId);
                if (sourceIndex !== -1) {
                    try {
                        const updatedSource = await api.get(`/api/sources/${sourceId}`);
                        this.sources.splice(sourceIndex, 1, updatedSource);
                    } catch (err) {
                        console.error('Failed to reload source in list:', err);
                    }
                }

                if (this.selectedSource && this.selectedSource.id === sourceId) {
                    try {
                        const updatedSource = await api.get(`/api/sources/${sourceId}`);
                        this.selectedSource = updatedSource;
                        if (success) {
                            await this.loadFragments();
                        }
                    } catch (err) {
                        console.error('Failed to reload source:', err);
                    }
                }
            });

            try {
                await this.signalRConnection.start();
                console.log('SignalR connected for fragment notifications');
            } catch (err) {
                console.error('SignalR connection error:', err);
            }

            window.addEventListener('keydown', this.handleKeydown);

            this.detachPopState = setupPopStateHandler(async () => {
                const sourceId = getUrlParam('id');
                if (sourceId) {
                    const source = findInList(this.sources, sourceId);
                    if (source) {
                        this.selectedSourceId = source.id;
                        this.selectedSource = await api.get(`/api/sources/${source.id}`);
                    }
                } else {
                    this.selectedSourceId = null;
                    this.selectedSource = null;
                }
            });
        },

        beforeUnmount() {
            window.removeEventListener('keydown', this.handleKeydown);
            if (this.signalRConnection) {
                this.signalRConnection.stop();
            }
            if (this.detachPopState) {
                this.detachPopState();
            }
        }
    });

    app.mount('#app');
})();

