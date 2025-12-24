// Fragments page Vue app (extracted from Fragments/Index.cshtml)
(function () {
    const { createApp } = Vue;
    const { api } = window.MedleyApi;
    const {
        formatDate,
        debounce,
        getFragmentCategoryIcon,
        getIconClass,
        getConfidenceIcon,
        getConfidenceColor,
        getConfidenceLabel,
        initializeMarkdownRenderer,
        findInList
    } = window.MedleyUtils;
    const {
        getUrlParam,
        setUrlParam,
        setupPopStateHandler
    } = window.UrlUtils;

    const app = createApp({
        mixins: [window.infiniteScrollMixin],
        components: {
            'fragment-list': FragmentList
        },
        data() {
            return {
                fragments: [],
                selectedFragmentId: null,
                selectedFragment: null,
                loading: false,
                searching: false,
                error: null,
                searchQuery: '',
                articleTypes: [],
                markdownRenderer: null,
                searchDebounced: null,
                showConfidenceComment: false,
                detachPopState: null
            };
        },
        computed: {
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
            }
        },
        methods: {
            async loadFragments() {
                this.resetPagination();
                this.loading = true;
                this.error = null;
                try {
                    const fragments = await api.get(`/api/fragments?skip=0&take=${this.pagination.pageSize}`);
                    this.fragments = fragments;
                    this.updateHasMore(fragments);
                } catch (err) {
                    this.error = 'Failed to load fragments: ' + err.message;
                    console.error('Error loading fragments:', err);
                } finally {
                    this.loading = false;
                }
            },

            async selectFragment(fragment, replaceState = false) {
                this.selectedFragmentId = fragment.id;
                this.showConfidenceComment = false;

                const currentId = getUrlParam('id');
                if (currentId !== fragment.id) {
                    setUrlParam('id', fragment.id, replaceState);
                }

                try {
                    this.selectedFragment = await api.get(`/api/fragments/${fragment.id}`);
                } catch (err) {
                    console.error('Error loading fragment:', err);
                    this.selectedFragment = null;
                }
            },

            toggleConfidenceComment() {
                this.showConfidenceComment = !this.showConfidenceComment;
            },

            onSearchInput() {
                if (this.searchDebounced) {
                    this.searchDebounced();
                }
            },

            async performSearch() {
                const query = this.searchQuery.trim();
                if (query.length >= 2) {
                    this.resetPagination();
                    this.searching = true;
                    try {
                        // Load all search results at once (no pagination for semantic search)
                        const fragments = await api.get(`/api/fragments/search?query=${encodeURIComponent(query)}&take=100`);
                        this.fragments = fragments;
                        // Disable infinite scroll for search results
                        this.pagination.hasMore = false;
                    } catch (err) {
                        console.error('Search error:', err);
                        this.error = 'Search failed: ' + err.message;
                    } finally {
                        this.searching = false;
                    }
                } else if (query.length === 0) {
                    await this.loadFragments();
                }
            },

            async loadMoreItems() {
                // Only load more for regular list view, not for search results
                const query = this.searchQuery.trim();
                if (query.length >= 2) {
                    // Search results don't support pagination - return empty to stop loading
                    return [];
                }
                
                const skip = this.pagination.page * this.pagination.pageSize;
                try {
                    const fragments = await api.get(`/api/fragments?skip=${skip}&take=${this.pagination.pageSize}`);
                    this.fragments.push(...fragments);
                    return fragments;
                } catch (err) {
                    console.error('Error loading more fragments:', err);
                    throw err;
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
            getConfidenceIcon,
            getConfidenceColor,
            getConfidenceLabel,
            formatDate
        },

        async mounted() {
            this.markdownRenderer = initializeMarkdownRenderer();

            this.searchDebounced = debounce(() => {
                this.performSearch();
            }, 500);

            await this.loadArticleTypes();
            await this.loadFragments();

            // Setup infinite scroll
            this.setupInfiniteScroll('.sidebar-content');

            const fragmentIdFromUrl = getUrlParam('id');
            if (fragmentIdFromUrl) {
                const fragment = findInList(this.fragments, fragmentIdFromUrl);
                if (fragment) {
                    await this.selectFragment(fragment, true);
                } else {
                    try {
                        const loadedFragment = await api.get(`/api/fragments/${fragmentIdFromUrl}`);
                        this.fragments.unshift(loadedFragment);
                        this.selectedFragment = loadedFragment;
                        this.selectedFragmentId = fragmentIdFromUrl;
                    } catch (err) {
                        console.error('Error loading fragment from URL:', err);
                        setUrlParam('id', null, true);
                    }
                }
            }

            this.detachPopState = setupPopStateHandler(async () => {
                const fragmentId = getUrlParam('id');
                if (fragmentId) {
                    const fragment = findInList(this.fragments, fragmentId);
                    if (fragment) {
                        this.selectedFragmentId = fragment.id;
                        this.selectedFragment = await api.get(`/api/fragments/${fragment.id}`);
                    }
                } else {
                    this.selectedFragmentId = null;
                    this.selectedFragment = null;
                }
            });
        },

        beforeUnmount() {
            if (this.detachPopState) {
                this.detachPopState();
            }
        }
    });

    app.mount('#app');
})();

