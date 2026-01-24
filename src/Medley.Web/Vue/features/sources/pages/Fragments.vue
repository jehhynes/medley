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
        <div class="d-flex justify-content-between align-items-start">
          <div>
            <h1 class="main-content-title">{{ selectedFragment.title || 'Untitled Fragment' }}</h1>
          </div>
        </div>
      </div>

      <div class="fragment-content-area">
        <fragment-body 
          :fragment="selectedFragment" 
          @updated="handleFragmentUpdated"
          @deleted="handleFragmentDeleted"
        />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onBeforeUnmount, watch } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import FragmentBody from '../components/FragmentBody.vue';

// Define props to handle attributes passed by router
defineProps<{
  id?: string;
}>();
import { fragmentsClient } from '@/utils/apiClients';
import { 
  getArticleTypes, 
  findInList, 
  debounce
} from '@/utils/helpers';
import { useSidebarState } from '@/composables/useSidebarState';
import { useInfiniteScroll } from '@/composables/useInfiniteScroll';
import type { FragmentDto } from '@/types/api-client';

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
const route = useRoute();

// Reactive state
const fragments = ref<FragmentDto[]>([]);
const selectedFragmentId = ref<string | null>(null);
const selectedFragment = ref<FragmentDto | null>(null);
const loading = ref<boolean>(false);
const searching = ref<boolean>(false);
const error = ref<string | null>(null);
const searchQuery = ref<string>('');
const userDisplayName = ref<string>(window.MedleyUser?.displayName || 'User');
const userIsAuthenticated = ref<boolean>(window.MedleyUser?.isAuthenticated || false);

let searchDebounced: (() => void) | null = null;

// Use infinite scroll composable
const {
  pagination,
  setupInfiniteScroll,
  resetPagination,
  updateHasMore
} = useInfiniteScroll({
  loadMoreItems,
  pageSize: 50
});

// Methods
const loadFragments = async (): Promise<void> => {
  resetPagination();
  loading.value = true;
  error.value = null;
  try {
    fragments.value = await fragmentsClient.getAll(0, pagination.value.pageSize);
    updateHasMore(fragments.value);
  } catch (err: any) {
    error.value = 'Failed to load fragments: ' + err.message;
    console.error('Error loading fragments:', err);
  } finally {
    loading.value = false;
  }
};

const selectFragment = async (fragment: FragmentDto, replaceState = false): Promise<void> => {
  selectedFragmentId.value = fragment.id!;

  if (replaceState) {
    await router.replace({ query: { id: fragment.id } });
  } else {
    await router.push({ query: { id: fragment.id } });
  }

  try {
    selectedFragment.value = await fragmentsClient.get(fragment.id!);
  } catch (err) {
    console.error('Error loading fragment:', err);
    selectedFragment.value = null;
  }
};

const handleFragmentUpdated = async (updatedFragment: FragmentDto): Promise<void> => {
  // Update the fragment in the local fragments array
  const index = fragments.value.findIndex(f => f.id === updatedFragment.id);
  if (index !== -1) {
    fragments.value[index] = updatedFragment;
  }
  // Update the selected fragment to show the new data
  selectedFragment.value = updatedFragment;
};

const handleFragmentDeleted = async (fragmentId: string): Promise<void> => {
  // Remove the deleted fragment from the list
  fragments.value = fragments.value.filter(f => f.id !== fragmentId);
  
  // Clear selection
  selectedFragment.value = null;
  selectedFragmentId.value = null;
  
  // Clear URL query
  await router.replace({ query: {} });
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
      fragments.value = await fragmentsClient.search(query, 100);
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

async function loadMoreItems(): Promise<FragmentDto[]> {
  const query = searchQuery.value.trim();
  if (query.length >= 2) {
    return [];
  }
  
  const skip = pagination.value.page * pagination.value.pageSize;
  try {
    const newFragments = await fragmentsClient.getAll(skip, pagination.value.pageSize);
    fragments.value.push(...newFragments);
    return newFragments;
  } catch (err) {
    console.error('Error loading more fragments:', err);
    throw err;
  }
}

// Lifecycle hooks
onMounted(async () => {
  searchDebounced = debounce(() => {
    performSearch();
  }, 500);

  getArticleTypes();
  
  await loadFragments();

  setupInfiniteScroll('.sidebar-content');

  const fragmentIdFromUrl = route.query.id as string | undefined;
  if (fragmentIdFromUrl) {
    const fragment = findInList(fragments.value, fragmentIdFromUrl);
    if (fragment) {
      await selectFragment(fragment, true);
    } else {
      try {
        const loadedFragment = await fragmentsClient.get(fragmentIdFromUrl);
        fragments.value.unshift(loadedFragment);
        selectedFragment.value = loadedFragment;
        selectedFragmentId.value = fragmentIdFromUrl;
      } catch (err) {
        console.error('Error loading fragment from URL:', err);
        await router.replace({ query: {} });
      }
    }
  }
});

// Watch for route changes (browser back/forward)
watch(() => route.query.id, async (newId) => {
  if (newId && typeof newId === 'string') {
    const fragment = findInList(fragments.value, newId);
    if (fragment) {
      selectedFragmentId.value = fragment.id!;
      try {
        selectedFragment.value = await fragmentsClient.get(fragment.id!);
      } catch (err) {
        console.error('Error loading fragment:', err);
      }
    }
  } else {
    selectedFragmentId.value = null;
    selectedFragment.value = null;
  }
});

onBeforeUnmount(() => {
  // Cleanup if needed
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
