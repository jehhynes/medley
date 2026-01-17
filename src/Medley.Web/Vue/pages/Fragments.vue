<template>
  <vertical-menu 
    :display-name="userDisplayName"
    :is-authenticated="userIsAuthenticated"
  />

  <!-- Left Sidebar (List) -->
  <div class="sidebar left-sidebar" :class="{ 'show': leftSidebarVisible }">
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

<script setup lang="ts">
import { ref, computed, onMounted, onBeforeUnmount } from 'vue';
import { useRouter } from 'vue-router';
import { api } from '@/utils/api';
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
} from '@/utils/helpers';
import { getUrlParam, setUrlParam, setupPopStateHandler } from '@/utils/url';
import { useSidebarState } from '@/composables/useSidebarState';
import { useInfiniteScroll } from '@/composables/useInfiniteScroll';
import type { Fragment } from '@/types/generated/api-client';

// Interfaces
interface PaginationState {
  page: number;
  pageSize: number;
  hasMore: boolean;
  loadingMore: boolean;
}

// Setup composables
const { leftSidebarVisible } = useSidebarState();
const router = useRouter();

// Reactive state
const fragments = ref<Fragment[]>([]);
const selectedFragmentId = ref<string | null>(null);
const selectedFragment = ref<Fragment | null>(null);
const loading = ref<boolean>(false);
const searching = ref<boolean>(false);
const error = ref<string | null>(null);
const searchQuery = ref<string>('');
const markdownRenderer = ref<any>(null);
const showConfidenceComment = ref<boolean>(false);
const userDisplayName = ref<string>(window.MedleyUser?.displayName || 'User');
const userIsAuthenticated = ref<boolean>(window.MedleyUser?.isAuthenticated || false);

const pagination = ref<PaginationState>({
  page: 0,
  pageSize: 50,
  hasMore: true,
  loadingMore: false
});

let searchDebounced: (() => void) | null = null;
let detachPopState: (() => void) | null = null;

// Use infinite scroll composable
const {
  setupInfiniteScroll,
  resetPagination,
  updateHasMore
} = useInfiniteScroll(pagination, loadMoreItems);

// Computed properties
const renderedMarkdown = computed(() => {
  if (!selectedFragment.value || !selectedFragment.value.content || !markdownRenderer.value) {
    return '';
  }
  try {
    return markdownRenderer.value.parse(selectedFragment.value.content, { breaks: true, gfm: true });
  } catch (e) {
    console.error('Failed to render markdown:', e);
    return selectedFragment.value.content;
  }
});

// Methods
const loadFragments = async (): Promise<void> => {
  resetPagination();
  loading.value = true;
  error.value = null;
  try {
    const data = await api.get(`/api/fragments?skip=0&take=${pagination.value.pageSize}`);
    fragments.value = data as Fragment[];
    updateHasMore(data as Fragment[]);
  } catch (err: any) {
    error.value = 'Failed to load fragments: ' + err.message;
    console.error('Error loading fragments:', err);
  } finally {
    loading.value = false;
  }
};

const selectFragment = async (fragment: Fragment, replaceState = false): Promise<void> => {
  selectedFragmentId.value = fragment.id;
  showConfidenceComment.value = false;

  if (replaceState) {
    await router.replace({ query: { id: fragment.id } });
  } else {
    await router.push({ query: { id: fragment.id } });
  }

  try {
    const data = await api.get(`/api/fragments/${fragment.id}`);
    selectedFragment.value = data as Fragment;
  } catch (err) {
    console.error('Error loading fragment:', err);
    selectedFragment.value = null;
  }
};

const toggleConfidenceComment = (): void => {
  showConfidenceComment.value = !showConfidenceComment.value;
};

const onSearchInput = (): void => {
  if (searchDebounced) {
    searchDebounced();
  }
};

const performSearch = async (): Promise<void> => {
  const query = searchQuery.value.trim();
  if (query.length >= 2) {
    resetPagination();
    searching.value = true;
    try {
      const data = await api.get(`/api/fragments/search?query=${encodeURIComponent(query)}&take=100`);
      fragments.value = data as Fragment[];
      pagination.value.hasMore = false;
    } catch (err: any) {
      console.error('Search error:', err);
      error.value = 'Search failed: ' + err.message;
    } finally {
      searching.value = false;
    }
  } else if (query.length === 0) {
    await loadFragments();
  }
};

async function loadMoreItems(): Promise<Fragment[]> {
  const query = searchQuery.value.trim();
  if (query.length >= 2) {
    return [];
  }
  
  const skip = pagination.value.page * pagination.value.pageSize;
  try {
    const data = await api.get(`/api/fragments?skip=${skip}&take=${pagination.value.pageSize}`);
    const newFragments = data as Fragment[];
    fragments.value.push(...newFragments);
    return newFragments;
  } catch (err) {
    console.error('Error loading more fragments:', err);
    throw err;
  }
}

// Lifecycle hooks
onMounted(async () => {
  markdownRenderer.value = initializeMarkdownRenderer();

  searchDebounced = debounce(() => {
    performSearch();
  }, 500);

  getArticleTypes();
  
  await loadFragments();

  setupInfiniteScroll('.sidebar-content');

  const fragmentIdFromUrl = getUrlParam('id');
  if (fragmentIdFromUrl) {
    const fragment = findInList(fragments.value, fragmentIdFromUrl);
    if (fragment) {
      await selectFragment(fragment, true);
    } else {
      try {
        const loadedFragment = await api.get(`/api/fragments/${fragmentIdFromUrl}`) as Fragment;
        fragments.value.unshift(loadedFragment);
        selectedFragment.value = loadedFragment;
        selectedFragmentId.value = fragmentIdFromUrl;
      } catch (err) {
        console.error('Error loading fragment from URL:', err);
        setUrlParam('id', null, true);
      }
    }
  }

  detachPopState = setupPopStateHandler(async () => {
    const fragmentId = getUrlParam('id');
    if (fragmentId) {
      const fragment = findInList(fragments.value, fragmentId);
      if (fragment) {
        selectedFragmentId.value = fragment.id;
        const data = await api.get(`/api/fragments/${fragment.id}`);
        selectedFragment.value = data as Fragment;
      }
    } else {
      selectedFragmentId.value = null;
      selectedFragment.value = null;
    }
  });
});

onBeforeUnmount(() => {
  if (detachPopState) {
    detachPopState();
  }
});
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
