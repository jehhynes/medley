<template>
  <vertical-menu 
    :display-name="userDisplayName"
    :is-authenticated="userIsAuthenticated"
    :is-open="openSidebarOnMobile"
  />

  <!-- Left Sidebar (List) -->
  <div class="sidebar left-sidebar" :class="{ 'show': openSidebarOnMobile }">
    <div class="sidebar-header">
      <h6 class="sidebar-title">Fragments</h6>
      <div class="input-group input-group-sm">
        <input type="text" 
               class="form-control" 
               placeholder="Search..." 
               v-model="searchQuery" 
               @input="onSearchInput"
               @keydown.enter="performSearch">
        <button class="btn btn-outline-secondary" type="button" @click="performSearch" :disabled="searching">
          <span v-if="searching" class="spinner-border spinner-border-sm" role="status">
            <span class="visually-hidden">Searching...</span>
          </span>
          <i v-else class="bi bi-search"></i>
        </button>
      </div>
    </div>
    <div class="sidebar-content">
      <div v-if="loading" class="loading-spinner">
        <div class="spinner-border spinner-border-sm" role="status">
          <span class="visually-hidden">Loading...</span>
        </div>
      </div>
      <div v-else-if="error" class="alert alert-danger m-2" v-cloak>
        {{ error }}
      </div>
      <fragment-list 
        v-else
        :fragments="fragments"
        :selected-id="selectedFragmentId"
        @select="selectFragment"
      />
      <div v-if="pagination && pagination.loadingMore" class="text-center py-3">
        <div class="spinner-border spinner-border-sm text-secondary" role="status">
          <span class="visually-hidden">Loading more...</span>
        </div>
      </div>
      <div v-if="!loading && fragments.length === 0" class="empty-state" v-cloak>
        <div class="empty-state-icon">
          <i class="bi bi-puzzle"></i>
        </div>
        <div class="empty-state-title">No Fragments Found</div>
        <div class="empty-state-text" v-if="searchQuery">No fragments match your search</div>
        <div class="empty-state-text" v-else>Extract fragments from sources to get started</div>
      </div>
    </div>
  </div>

  <!-- Main Content -->
  <div class="main-content">
    <div v-if="!selectedFragment" class="empty-state" v-cloak>
      <div class="empty-state-icon">
        <i class="bi bi-puzzle"></i>
      </div>
      <div class="empty-state-title">No Fragment Selected</div>
      <div class="empty-state-text">Select a fragment from the sidebar to view its details</div>
    </div>
    <div v-else class="d-flex flex-column h-100">
      <div class="main-content-header">
        <div class="d-flex justify-content-between align-items-start mb-3">
          <div>
            <h1 class="main-content-title">{{ selectedFragment.title || 'Untitled Fragment' }}</h1>
            <div class="text-muted">
              <span class="badge bg-secondary" v-if="selectedFragment.category">
                <i :class="getIconClass(getFragmentCategoryIcon(selectedFragment.category))" class="me-1"></i>{{ selectedFragment.category }}
              </span>
              <span 
                v-if="selectedFragment.confidence !== null && selectedFragment.confidence !== undefined && selectedFragment.confidenceComment" 
                class="badge bg-light text-dark ms-2"
                @click="toggleConfidenceComment"
                style="cursor: pointer;"
                :title="showConfidenceComment ? 'Hide confidence note' : 'Show confidence note'">
                <i 
                  :class="'fa-duotone ' + getConfidenceIcon(selectedFragment.confidence)" 
                  :style="{ color: getConfidenceColor(selectedFragment.confidence) }"
                  class="me-1"
                ></i>
                {{ selectedFragment.confidence || '' }}
                <i :class="showConfidenceComment ? 'bi bi-chevron-up ms-1' : 'bi bi-chevron-down ms-1'"></i>
              </span>
              <span 
                v-else-if="selectedFragment.confidence !== null && selectedFragment.confidence !== undefined" 
                class="badge bg-light text-dark ms-2">
                <i 
                  :class="'fa-duotone ' + getConfidenceIcon(selectedFragment.confidence)" 
                  :style="{ color: getConfidenceColor(selectedFragment.confidence) }"
                  class="me-1"
                ></i>
                {{ selectedFragment.confidence || '' }}
              </span>
              <span class="ms-2">
                <i class="bi bi-calendar3"></i>
                {{ formatDate(selectedFragment.sourceDate) }}
              </span>
              <a :href="'/Sources?id=' + selectedFragment.sourceId" class="ms-2 source-link">
                <i class="bi bi-camera-video me-1"></i>{{ selectedFragment.sourceName || 'View Source' }}
              </a>
            </div>
          </div>
        </div>
      </div>

      <div class="fragment-content-area">
        <div v-if="selectedFragment.confidenceComment && showConfidenceComment" class="alert alert-info mb-3">
          <div class="d-flex align-items-start">
            <i class="bi bi-info-circle me-2 mt-1"></i>
            <div>
              <strong>Confidence Note:</strong>
              <div class="mt-1">{{ selectedFragment.confidenceComment }}</div>
            </div>
          </div>
        </div>
        <div class="markdown-container" v-html="renderedMarkdown"></div>
      </div>
    </div>
  </div>
</template>

<script>
import infiniteScrollMixin from '../mixins/infiniteScroll.js';
import { api } from '@/utils/api.js';
import { 
  getFragmentCategoryIcon, 
  getIconClass, 
  getConfidenceIcon, 
  getConfidenceColor, 
  formatDate, 
  initializeMarkdownRenderer, 
  getArticleTypes, 
  findInList, 
  showToast,
  debounce
} from '@/utils/helpers.js';
import { getUrlParam, setUrlParam, setupPopStateHandler } from '@/utils/url.js';

export default {
  name: 'Fragments',
  mixins: [infiniteScrollMixin],
  data() {
    return {
      fragments: [],
      selectedFragmentId: null,
      selectedFragment: null,
      loading: false,
      searching: false,
      error: null,
      searchQuery: '',
      markdownRenderer: null,
      searchDebounced: null,
      showConfidenceComment: false,
      detachPopState: null,
      // User info from server
      userDisplayName: window.MedleyUser?.displayName || 'User',
      userIsAuthenticated: window.MedleyUser?.isAuthenticated || false,
      openSidebarOnMobile: window.MedleyUser?.openSidebarOnMobile || false
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

    getFragmentCategoryIcon(...args) {
      return getFragmentCategoryIcon(...args);
    },
    
    getIconClass(...args) {
      return getIconClass(...args);
    },
    
    getConfidenceIcon(...args) {
      return getConfidenceIcon(...args);
    },
    
    getConfidenceColor(...args) {
      return getConfidenceColor(...args);
    },
    
    formatDate(...args) {
      return formatDate(...args);
    }
  },

  async mounted() {
    this.markdownRenderer = initializeMarkdownRenderer();

    this.searchDebounced = debounce(() => {
      this.performSearch();
    }, 500);

    // Preload article types for icon display
    getArticleTypes();
    
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
};
</script>

<style scoped>
/* Make main content use flexbox for proper layout */
.main-content {
  display: flex;
  flex-direction: column;
  height: 100%;
  overflow: hidden;
  padding: 0;
}

.main-content-header {
  flex-shrink: 0;
  padding: 2rem 2.5rem 1.5rem 2.5rem;
  margin-bottom: 0!important;
}

/* Fragment content area - scrollable */
.fragment-content-area {
  flex: 1;
  overflow-y: auto;
  padding: 0 2.5rem 2rem 2.5rem;
}

/* Content preview - let it flow naturally */
.fragment-content {
  white-space: pre-wrap;
  word-wrap: break-word;
}
</style>
