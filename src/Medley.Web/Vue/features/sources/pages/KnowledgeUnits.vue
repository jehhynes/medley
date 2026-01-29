<template>
  <vertical-menu 
    :display-name="userDisplayName"
    :is-authenticated="userIsAuthenticated"
  />

  <!-- Left Sidebar (List) -->
  <div class="sidebar left-sidebar" :class="{ 'show': leftSidebarVisible }">
    <div class="sidebar-header">
      <h6 class="sidebar-title">Knowledge Units</h6>
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
      <knowledge-unit-list 
        v-else
        :knowledge-units="knowledgeUnits"
        :selected-id="selectedKnowledgeUnitId"
        @select="selectKnowledgeUnit"
      />
      <div v-if="pagination && pagination.loadingMore" class="text-center py-3">
        <div class="spinner-border spinner-border-sm text-secondary" role="status">
          <span class="visually-hidden">Loading more...</span>
        </div>
      </div>
      <div v-if="!loading && knowledgeUnits.length === 0" class="empty-state" v-cloak>
        <div class="empty-state-icon">
          <i class="fal fa-atom"></i>
        </div>
        <div class="empty-state-title">No Knowledge Units Found</div>
        <div class="empty-state-text" v-if="searchQuery">No knowledge units match your search</div>
        <div class="empty-state-text" v-else>Knowledge units will appear here after fragments are clustered</div>
      </div>
    </div>
  </div>

  <!-- Main Content -->
  <div class="main-content">
    <div v-if="!selectedKnowledgeUnit" class="empty-state" v-cloak>
      <div class="empty-state-icon">
        <i class="fal fa-atom"></i>
      </div>
      <div class="empty-state-title">No Knowledge Unit Selected</div>
      <div class="empty-state-text">Select a knowledge unit from the sidebar to view its details</div>
    </div>
    <knowledge-unit-body 
      v-else
      :knowledge-unit="selectedKnowledgeUnit" 
      @updated="handleKnowledgeUnitUpdated"
      @deleted="handleKnowledgeUnitDeleted"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onBeforeUnmount, watch } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import KnowledgeUnitBody from '../components/KnowledgeUnitBody.vue';
import KnowledgeUnitList from '../components/KnowledgeUnitList.vue';

// Define props to handle attributes passed by router
defineProps<{
  id?: string;
}>();
import { knowledgeUnitsClient } from '@/utils/apiClients';
import { 
  findInList, 
  debounce
} from '@/utils/helpers';
import { useSidebarState } from '@/composables/useSidebarState';
import { useInfiniteScroll } from '@/composables/useInfiniteScroll';
import type { KnowledgeUnitDto } from '@/types/api-client';

// Setup composables
const { leftSidebarVisible } = useSidebarState();
const router = useRouter();
const route = useRoute();

// Reactive state
const knowledgeUnits = ref<KnowledgeUnitDto[]>([]);
const selectedKnowledgeUnitId = ref<string | null>(null);
const selectedKnowledgeUnit = ref<KnowledgeUnitDto | null>(null);
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
const loadKnowledgeUnits = async (): Promise<void> => {
  resetPagination();
  loading.value = true;
  error.value = null;
  try {
    knowledgeUnits.value = await knowledgeUnitsClient.getAll(0, pagination.value.pageSize);
    updateHasMore(knowledgeUnits.value);
  } catch (err: any) {
    error.value = 'Failed to load knowledge units: ' + err.message;
    console.error('Error loading knowledge units:', err);
  } finally {
    loading.value = false;
  }
};

const selectKnowledgeUnit = async (knowledgeUnit: KnowledgeUnitDto, replaceState = false): Promise<void> => {
  // Update URL first
  if (replaceState) {
    await router.replace({ query: { id: knowledgeUnit.id } });
  } else {
    await router.push({ query: { id: knowledgeUnit.id } });
  }

  // Set the selected ID - this will trigger the route watcher which loads the full record
  selectedKnowledgeUnitId.value = knowledgeUnit.id!;
};

const handleKnowledgeUnitUpdated = async (updatedKnowledgeUnit: KnowledgeUnitDto): Promise<void> => {
  // Update the knowledge unit in the local array
  const index = knowledgeUnits.value.findIndex(ku => ku.id === updatedKnowledgeUnit.id);
  if (index !== -1) {
    knowledgeUnits.value[index] = updatedKnowledgeUnit;
  }
  // Update the selected knowledge unit to show the new data
  selectedKnowledgeUnit.value = updatedKnowledgeUnit;
};

const handleKnowledgeUnitDeleted = async (knowledgeUnitId: string): Promise<void> => {
  // Remove the deleted knowledge unit from the list
  knowledgeUnits.value = knowledgeUnits.value.filter(ku => ku.id !== knowledgeUnitId);
  
  // Clear selection
  selectedKnowledgeUnit.value = null;
  selectedKnowledgeUnitId.value = null;
  
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
      knowledgeUnits.value = await knowledgeUnitsClient.search(query, 100);
      pagination.value.hasMore = false;
    } catch (err: any) {
      console.error('Search error:', err);
      error.value = 'Search failed: ' + err.message;
    } finally {
      searching.value = false;
    }
  } else if (query.length === 0) {
    await loadKnowledgeUnits();
  }
};

async function loadMoreItems(): Promise<KnowledgeUnitDto[]> {
  const query = searchQuery.value.trim();
  if (query.length >= 2) {
    return [];
  }
  
  const skip = pagination.value.page * pagination.value.pageSize;
  try {
    const newKnowledgeUnits = await knowledgeUnitsClient.getAll(skip, pagination.value.pageSize);
    knowledgeUnits.value.push(...newKnowledgeUnits);
    return newKnowledgeUnits;
  } catch (err) {
    console.error('Error loading more knowledge units:', err);
    throw err;
  }
}

// Lifecycle hooks
onMounted(async () => {
  searchDebounced = debounce(() => {
    performSearch();
  }, 500);
  
  await loadKnowledgeUnits();

  setupInfiniteScroll('.sidebar-content');

  const knowledgeUnitIdFromUrl = route.query.id as string | undefined;
  if (knowledgeUnitIdFromUrl) {
    try {
      // Always load the full record from API
      const loadedKnowledgeUnit = await knowledgeUnitsClient.get(knowledgeUnitIdFromUrl);
      
      // Add to list if not already present
      const existingIndex = knowledgeUnits.value.findIndex(ku => ku.id === knowledgeUnitIdFromUrl);
      if (existingIndex === -1) {
        knowledgeUnits.value.unshift(loadedKnowledgeUnit);
      } else {
        knowledgeUnits.value[existingIndex] = loadedKnowledgeUnit;
      }
      
      // Set the selected knowledge unit
      selectedKnowledgeUnit.value = loadedKnowledgeUnit;
      selectedKnowledgeUnitId.value = knowledgeUnitIdFromUrl;
    } catch (err) {
      console.error('Error loading knowledge unit from URL:', err);
      await router.replace({ query: {} });
    }
  }
});

// Watch for route changes (browser back/forward)
watch(() => route.query.id, async (newId) => {
  if (newId && typeof newId === 'string') {
    const knowledgeUnit = findInList(knowledgeUnits.value, newId);
    if (knowledgeUnit) {
      selectedKnowledgeUnitId.value = knowledgeUnit.id!;
    }
    // Always load the full record from API
    try {
      selectedKnowledgeUnit.value = await knowledgeUnitsClient.get(newId);
    } catch (err) {
      console.error('Error loading knowledge unit:', err);
      selectedKnowledgeUnit.value = null;
    }
  } else {
    selectedKnowledgeUnitId.value = null;
    selectedKnowledgeUnit.value = null;
  }
});

onBeforeUnmount(() => {
  // Cleanup if needed
});
</script>

<style scoped>
</style>
