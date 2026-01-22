<template>
    <vertical-menu 
      :display-name="userDisplayName"
      :is-authenticated="userIsAuthenticated"
    />

    <!-- Left Sidebar (List) -->
    <div class="sidebar left-sidebar" :class="{ 'show': leftSidebarVisible }">
      <div class="sidebar-header">
        <h6 class="sidebar-title">Sources</h6>
        <div v-if="activeTagFilter" class="mb-2">
          <div class="d-flex align-items-center justify-content-between">
            <span class="badge bg-info rounded-pill">
              <i class="bi bi-tag-fill me-1"></i>{{ activeTagFilter.value }}
            </span>
            <button class="btn btn-sm btn-link text-muted p-0 ms-2" @click="clearTagFilter" title="Clear filter">
              <i class="bi bi-x-lg"></i>
            </button>
          </div>
        </div>
        <div class="input-group input-group-sm">
          <input type="text" class="form-control" placeholder="Search..." v-model="searchQuery" @input="onSearchInput">
        </div>
      </div>
      <div class="sidebar-content">
        <div v-if="loading" class="loading-spinner">
          <div class="spinner-border spinner-border-sm" role="status">
            <span class="visually-hidden">Loading...</span>
          </div>
        </div>
        <div v-else-if="error" class="alert alert-danger" v-cloak>
          {{ error }}
        </div>
        <source-list 
          v-else
          :sources="sources"
          :selected-id="selectedSourceId"
          @select="selectSource"
        />
        <div v-if="pagination && pagination.loadingMore" class="text-center py-3">
          <div class="spinner-border spinner-border-sm text-secondary" role="status">
            <span class="visually-hidden">Loading more...</span>
          </div>
        </div>
        <div v-if="!loading && sources.length === 0" class="empty-state" v-cloak>
          <div class="empty-state-icon">
            <i class="bi bi-camera-video"></i>
          </div>
          <div class="empty-state-title" v-if="activeTagFilter">No Sources with This Tag</div>
          <div class="empty-state-title" v-else>No Sources Found</div>
          <div class="empty-state-text" v-if="activeTagFilter">Try selecting a different tag or clear the filter</div>
          <div class="empty-state-text" v-else>Connect an integration to import sources</div>
        </div>
      </div>
    </div>

    <!-- Main Content -->
    <div class="main-content">
      <div v-if="!selectedSource" class="empty-state" v-cloak>
        <div class="empty-state-icon">
          <i class="bi bi-camera-video"></i>
        </div>
        <div class="empty-state-title">No Source Selected</div>
        <div class="empty-state-text">Select a source from the sidebar to view its details</div>
      </div>
      <div v-else class="d-flex flex-column h-100">
        <div class="main-content-header">
          <div class="d-flex justify-content-between align-items-start mb-3">
            <div>
              <h1 class="main-content-title">{{ selectedSource.name || 'Untitled Source' }}</h1>
              <div class="text-muted">
                <span class="badge bg-primary">
                  <i :class="getIconClass(getSourceTypeIcon(selectedSource.type))" class="me-1"></i>{{ selectedSource.type }}
                </span>
                <span class="ms-2">
                  <i class="bi bi-calendar3"></i>
                  {{ formatDate(selectedSource.date) }}
                </span>
                <span class="ms-2">
                  <i class="bi bi-plug"></i>
                  {{ selectedSource.integrationName }}
                </span>
                <span class="ms-2" v-if="selectedSource.fragmentsCount !== undefined">
                  <i class="bi bi-puzzle"></i>
                  {{ selectedSource.fragmentsCount }} fragments
                </span>
                <span class="ms-2" v-if="selectedSource.primarySpeakerName">
                  <i class="bi bi-person"></i>
                  {{ selectedSource.primarySpeakerName }}
                  <i 
                    v-if="selectedSource.primarySpeakerTrustLevel" 
                    class="bi bi-shield-check ms-1" 
                    :class="getTrustLevelClass(selectedSource.primarySpeakerTrustLevel)"
                  ></i>
                </span>
              </div>
              <div class="mt-2" v-if="sortedTags.length || selectedSource.isInternal">
                <span
                  v-if="selectedSource.isInternal"
                  class="badge rounded-pill bg-success me-1 mb-1">
                  Internal
                </span>
                <span 
                  v-for="tag in sortedTags" 
                  :key="tag.tagTypeId"
                  class="badge rounded-pill bg-info me-1 mb-1"
                  @click.stop="filterByTag(tag)"
                  style="cursor: pointer;"
                  :title="activeTagFilter && activeTagFilter.tagTypeId === tag.tagTypeId && activeTagFilter.value === tag.value ? 'Click to clear filter' : 'Click to filter by this tag'">
                  {{ tag.value }}
                </span>
              </div>
            </div>
            <div>
              <button 
                class="btn btn-primary" 
                @click="extractFragments" 
                :disabled="selectedSource.extractionStatus === 'InProgress'"
                v-if="selectedSource.content">
                <span v-if="selectedSource.extractionStatus === 'InProgress'" class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>
                <i v-else class="bi bi-magic"></i> 
                {{ selectedSource.extractionStatus === 'Completed' ? 'Re-extract Fragments' : 'Extract Fragments' }}
              </button>
              <button 
                class="btn btn-outline-secondary ms-2" 
                @click="generateTags" 
                :disabled="tagging || !selectedSource"
                v-if="!selectedSource.tagsGenerated">
                <span v-if="tagging" class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>
                <i v-else class="bi bi-tags"></i> Generate Tags
              </button>
            </div>
          </div>
        </div>

        <ul class="nav nav-tabs" role="tablist">
          <li class="nav-item" role="presentation">
            <button class="nav-link active" id="content-tab" data-bs-toggle="tab" data-bs-target="#content-pane" type="button" role="tab" aria-controls="content-pane" aria-selected="true">
              Content
            </button>
          </li>
          <li class="nav-item" role="presentation">
            <button class="nav-link" id="fragments-tab" data-bs-toggle="tab" data-bs-target="#fragments-pane" type="button" role="tab" aria-controls="fragments-pane" aria-selected="false">
              Fragments
            </button>
          </li>
          <li class="nav-item" role="presentation">
            <button class="nav-link" id="metadata-tab" data-bs-toggle="tab" data-bs-target="#metadata-pane" type="button" role="tab" aria-controls="metadata-pane" aria-selected="false">
              Metadata
            </button>
          </li>
        </ul>

        <div class="tab-content">
          <div class="tab-pane show active" id="content-pane" role="tabpanel" aria-labelledby="content-tab">
            <div v-if="selectedSource.content">
              <div class="content-preview">
                {{ selectedSource.content }}
              </div>
            </div>
            <div v-else class="text-muted">
              No content available
            </div>
          </div>
          <div class="tab-pane" id="fragments-pane" role="tabpanel" aria-labelledby="fragments-tab">
            <div v-if="loadingFragments" class="text-center py-4">
              <div class="spinner-border spinner-border-sm" role="status">
                <span class="visually-hidden">Loading...</span>
              </div>
            </div>
            <div v-else-if="fragmentsError" class="alert alert-danger" v-cloak>
              {{ fragmentsError }}
            </div>
            <template v-else>
              <div v-if="selectedSource && selectedSource.extractionMessage && fragments.length === 0" class="alert alert-info mb-3" v-cloak>
                <div class="d-flex align-items-start">
                  <i class="bi bi-info-circle me-2 mt-1"></i>
                  <div class="flex-grow-1">
                    <strong>Extraction Message:</strong>
                    <div class="mt-1">{{ selectedSource.extractionMessage }}</div>
                  </div>
                </div>
              </div>
              <div v-if="fragments.length === 0 && (!selectedSource || !selectedSource.extractionMessage)" class="text-muted">
                No fragments available. Click "Extract Fragments" to generate fragments from this source.
              </div>
              <div v-else class="fragment-list">
                <table class="table table-hover">
                  <tbody>
                    <tr 
                      v-for="fragment in fragments" 
                      :key="fragment.id"
                      class="fragment-item"
                      @click="selectFragment(fragment)"
                      style="cursor: pointer;"
                    >
                      <td class="align-middle" style="width: 50px;">
                        <i 
                          :class="getIconClass(getFragmentCategoryIcon(fragment.category))" 
                          :title="fragment.category || ''"
                          style="font-size: 1.25rem;"
                        ></i>
                      </td>
                      <td>
                        <div class="fw-semibold">{{ fragment.title || 'Untitled Fragment' }}</div>
                        <div v-if="fragment.summary" class="text-muted small">
                          {{ fragment.summary }}
                        </div>
                      </td>
                      <td class="align-middle text-end" style="width: 100px;" v-if="fragment.confidence !== null && fragment.confidence !== undefined">
                        <i 
                          :class="'fa-duotone ' + getConfidenceIcon(fragment.confidence)" 
                          :style="{ color: getConfidenceColor(fragment.confidence) }"
                          :title="'Confidence: ' + (fragment.confidence || '')"
                          style="font-size: 1.25rem;"
                        ></i>
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </template>
          </div>
          <div class="tab-pane" id="metadata-pane" role="tabpanel" aria-labelledby="metadata-tab">
            <div v-if="parsedMetadata">
              <json-viewer :data="parsedMetadata"></json-viewer>
            </div>
            <div v-else class="text-muted">
              No metadata available
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Fragment Detail Modal -->
    <fragment-modal
      :fragment="selectedFragment"
      :visible="!!selectedFragment"
      @close="closeFragmentModal"
    />
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted, onBeforeUnmount } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { sourcesClient, fragmentsClient } from '@/utils/apiClients';
import { 
  getFragmentCategoryIcon, 
  getIconClass, 
  getSourceTypeIcon, 
  getConfidenceIcon, 
  getConfidenceColor, 
  formatDate, 
  initializeMarkdownRenderer, 
  getArticleTypes, 
  findInList, 
  showToast 
} from '@/utils/helpers';
import { useSidebarState } from '@/composables/useSidebarState';
import { useInfiniteScroll } from '@/composables/useInfiniteScroll';
import { createAdminHubConnection } from '@/utils/signalr';
import type { SourceDto, FragmentDto, TagDto } from '@/types/api-client';
import type { AdminHubConnection } from '@/types/admin-hub';

// Interfaces
interface PaginationState {
  page: number;
  pageSize: number;
  hasMore: boolean;
  loadingMore: boolean;
}

interface TagFilter {
  tagTypeId: number;
  value: string;
  tagType: string;
}

// Setup composables
const { leftSidebarVisible } = useSidebarState();
const router = useRouter();
const route = useRoute();

// Reactive state
const sources = ref<SourceDto[]>([]);
const selectedSourceId = ref<string | null>(null);
const selectedSource = ref<SourceDto | null>(null);
const loading = ref<boolean>(false);
const error = ref<string | null>(null);
const searchQuery = ref<string>('');
const signalRConnection = ref<AdminHubConnection | null>(null);
const fragments = ref<FragmentDto[]>([]);
const selectedFragment = ref<FragmentDto | null>(null);
const loadingFragments = ref<boolean>(false);
const fragmentsError = ref<string | null>(null);
const markdownRenderer = ref<any>(null);
const tagging = ref<boolean>(false);
const activeTagFilter = ref<TagFilter | null>(null);
const userDisplayName = ref<string>(window.MedleyUser?.displayName || 'User');
const userIsAuthenticated = ref<boolean>(window.MedleyUser?.isAuthenticated || false);

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

// Computed properties
const parsedMetadata = computed(() => {
  if (!selectedSource.value || !selectedSource.value.metadataJson) {
    return null;
  }
  try {
    return JSON.parse(selectedSource.value.metadataJson);
  } catch (e) {
    console.error('Failed to parse metadata JSON:', e);
    return null;
  }
});

const sortedTags = computed(() => {
  if (!selectedSource.value || !selectedSource.value.tags) {
    return [];
  }
  return [...selectedSource.value.tags].sort((a, b) => {
    const tagTypeA = (a.tagType || '').toLowerCase();
    const tagTypeB = (b.tagType || '').toLowerCase();
    return tagTypeA.localeCompare(tagTypeB);
  });
});

// Watchers
watch(selectedSource, (newVal) => {
  fragments.value = [];
  selectedFragment.value = null;
  fragmentsError.value = null;

  if (newVal) {
    loadFragments();
  }
});

// Methods
const loadSources = async (): Promise<void> => {
  resetPagination();
  loading.value = true;
  error.value = null;
  try {
    let tagTypeId: string | undefined;
    let value: string | undefined;
    
    if (activeTagFilter.value) {
      tagTypeId = activeTagFilter.value.tagTypeId.toString();
      value = activeTagFilter.value.value;
    }
    
    sources.value = await sourcesClient.getAll(undefined, tagTypeId, value, 0, pagination.value.pageSize);
    updateHasMore(sources.value);
  } catch (err: any) {
    error.value = 'Failed to load sources: ' + err.message;
    console.error('Error loading sources:', err);
  } finally {
    loading.value = false;
  }
};

async function loadMoreItems(): Promise<SourceDto[]> {
  const skip = pagination.value.page * pagination.value.pageSize;
  try {
    const query = searchQuery.value.trim();
    let tagTypeId: string | undefined;
    let value: string | undefined;
    
    if (activeTagFilter.value) {
      tagTypeId = activeTagFilter.value.tagTypeId.toString();
      value = activeTagFilter.value.value;
    }
    
    const newSources = await sourcesClient.getAll(
      query.length >= 2 ? query : undefined,
      tagTypeId,
      value,
      skip,
      pagination.value.pageSize
    );
    sources.value.push(...newSources);
    return newSources;
  } catch (err) {
    console.error('Error loading more sources:', err);
    throw err;
  }
}

const selectSource = async (source: SourceDto, replaceState = false): Promise<void> => {
  selectedSourceId.value = source.id!;

  if (replaceState) {
    await router.replace({ query: { id: source.id } });
  } else {
    await router.push({ query: { id: source.id } });
  }

  try {
    selectedSource.value = await sourcesClient.get(source.id!);
    const sourceIndex = sources.value.findIndex(s => s.id === source.id);
    if (sourceIndex !== -1 && selectedSource.value.tags) {
      sources.value[sourceIndex].tags = selectedSource.value.tags;
    }
  } catch (err) {
    console.error('Error loading source:', err);
    selectedSource.value = null;
  }
};

const onSearchInput = async (): Promise<void> => {
  const query = searchQuery.value.trim();
  if (query.length >= 2) {
    resetPagination();
    try {
      let tagTypeId: string | undefined;
      let value: string | undefined;
      
      if (activeTagFilter.value) {
        tagTypeId = activeTagFilter.value.tagTypeId.toString();
        value = activeTagFilter.value.value;
      }
      
      sources.value = await sourcesClient.getAll(query, tagTypeId, value, 0, pagination.value.pageSize);
      updateHasMore(sources.value);
    } catch (err) {
      console.error('Search error:', err);
    }
  } else if (query.length === 0) {
    await loadSources();
  }
};

const filterByTag = async (tag: TagDto): Promise<void> => {
  if (activeTagFilter.value && 
      activeTagFilter.value.tagTypeId === tag.tagTypeId && 
      activeTagFilter.value.value === tag.value) {
    await clearTagFilter();
  } else {
    activeTagFilter.value = {
      tagTypeId: tag.tagTypeId!,
      value: tag.value!,
      tagType: tag.tagType!
    };
    searchQuery.value = '';
    await loadSources();
  }
};

const clearTagFilter = async (): Promise<void> => {
  activeTagFilter.value = null;
  if (searchQuery.value.trim().length >= 2) {
    await onSearchInput();
  } else {
    await loadSources();
  }
};

const extractFragments = async (): Promise<void> => {
  if (!selectedSource.value) return;
  const sourceId = selectedSource.value.id;

  if (fragments.value.length > 0) {
    const confirmMessage = `This source already has ${fragments.value.length} fragment(s). ` +
      'Re-extracting will delete existing fragments. Continue?';

    (window as any).bootbox.confirm({
      title: 'Confirm Fragment Extraction',
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
      callback: async (result: boolean) => {
        if (result) {
          await performExtraction(sourceId);
        }
      }
    });
  } else {
    await performExtraction(sourceId);
  }
};

const performExtraction = async (sourceId: string): Promise<void> => {
  if (selectedSource.value) {
    selectedSource.value.extractionStatus = 'InProgress';
  }
  const sourceIndex = sources.value.findIndex(s => s.id === sourceId);
  if (sourceIndex !== -1) {
    sources.value[sourceIndex].extractionStatus = 'InProgress';
  }

  try {
    const response = await sourcesClient.extractFragments(sourceId);

    if (!response.success) {
      showToast('error', response.message || 'Failed to start fragment extraction');
      if (selectedSource.value) {
        selectedSource.value.extractionStatus = 'NotStarted';
      }
      if (sourceIndex !== -1) {
        sources.value[sourceIndex].extractionStatus = 'NotStarted';
      }
    }
  } catch (err: any) {
    console.error('Fragment extraction error:', err);
    const errorMessage = err.message || 'Failed to extract fragments. Please try again.';
    const isClustered = errorMessage.toLowerCase().includes('clustered');

    if (isClustered) {
      showToast('error', 'Cannot re-extract: Some fragments have been clustered. Please uncluster them first.');
    } else {
      showToast('error', errorMessage);
    }

    if (selectedSource.value) {
      selectedSource.value.extractionStatus = 'NotStarted';
    }
    if (sourceIndex !== -1) {
      sources.value[sourceIndex].extractionStatus = 'NotStarted';
    }
  }
};

const generateTags = async (): Promise<void> => {
  if (!selectedSource.value) return;
  const sourceId = selectedSource.value.id!;
  tagging.value = true;
  try {
    const result = await sourcesClient.tagSource(sourceId, true);
    if (!result.success && result.message) {
      showToast('error', result.message);
    } else {
      showToast('success', result.message || 'Tags generated');
    }
    try {
      selectedSource.value = await sourcesClient.get(sourceId);
    } catch (err) {
      console.error('Failed to reload source after tagging:', err);
    }
  } catch (err: any) {
    console.error('Tag generation error:', err);
    showToast('error', err.message || 'Failed to generate tags. Please try again.');
  } finally {
    tagging.value = false;
  }
};

const loadFragments = async (): Promise<void> => {
  if (!selectedSource.value) return;

  loadingFragments.value = true;
  fragmentsError.value = null;
  try {
    fragments.value = await fragmentsClient.getBySourceId(selectedSource.value.id!);
  } catch (err: any) {
    fragmentsError.value = 'Failed to load fragments: ' + err.message;
    console.error('Error loading fragments:', err);
  } finally {
    loadingFragments.value = false;
  }
};

const selectFragment = (fragment: FragmentDto): void => {
  selectedFragment.value = fragment;
};

const closeFragmentModal = (): void => {
  selectedFragment.value = null;
};

const getTrustLevelClass = (trustLevel: string | null): string => {
  if (!trustLevel) return '';
  switch (trustLevel) {
    case 'High': return 'text-success';
    case 'Medium': return 'text-warning';
    case 'Low': return 'text-danger';
    default: return '';
  }
};

// Lifecycle hooks
onMounted(async () => {
  markdownRenderer.value = initializeMarkdownRenderer();

  getArticleTypes();
  
  await loadSources();

  setupInfiniteScroll('.sidebar-content');

  const sourceIdFromUrl = route.query.id as string | undefined;
  if (sourceIdFromUrl) {
    const source = findInList(sources.value, sourceIdFromUrl);
    if (source) {
      await selectSource(source, true);
    } else {
      try {
        const loadedSource = await sourcesClient.get(sourceIdFromUrl);
        sources.value.unshift(loadedSource);
        selectedSource.value = loadedSource;
        selectedSourceId.value = sourceIdFromUrl;
      } catch (err) {
        console.error('Error loading source from URL:', err);
        await router.replace({ query: {} });
      }
    }
  }

  signalRConnection.value = createAdminHubConnection();

  signalRConnection.value.on('FragmentExtractionComplete', async (sourceId: string, fragmentCount: number, success: boolean) => {
    const sourceIndex = sources.value.findIndex(s => s.id === sourceId);
    if (sourceIndex !== -1) {
      try {
        const updatedSource = await sourcesClient.get(sourceId);
        sources.value.splice(sourceIndex, 1, updatedSource);
      } catch (err) {
        console.error('Failed to reload source in list:', err);
      }
    }

    if (selectedSource.value && selectedSource.value.id === sourceId) {
      try {
        selectedSource.value = await sourcesClient.get(sourceId);
        if (success) {
          await loadFragments();
        }
      } catch (err) {
        console.error('Failed to reload source:', err);
      }
    }
  });

  try {
    await signalRConnection.value.start();
    console.log('SignalR connected for fragment notifications');
  } catch (err) {
    console.error('SignalR connection error:', err);
  }
});

// Watch for route changes (browser back/forward)
watch(() => route.query.id, async (newId) => {
  if (newId && typeof newId === 'string') {
    const source = findInList(sources.value, newId);
    if (source) {
      selectedSourceId.value = source.id!;
      try {
        selectedSource.value = await sourcesClient.get(source.id!);
      } catch (err) {
        console.error('Error loading source:', err);
      }
    }
  } else {
    selectedSourceId.value = null;
    selectedSource.value = null;
  }
});

onBeforeUnmount(() => {
  if (signalRConnection.value) {
    signalRConnection.value.stop();
  }
});
</script>

<style scoped>
json-viewer {
  --background-color: transparent;
}

/* Make main content use flexbox for independent tab scrolling */
.main-content {
  display: flex;
  flex-direction: column;
  height: 100%;
  overflow: hidden;
  padding: 0;
}

.main-content-header {
  flex-shrink: 0;
  padding: 2rem 2.5rem 0 2.5rem;
  margin-bottom: 0!important;
}

/* Tab navigation container */
.main-content .nav-tabs {
  flex-shrink: 0;
  margin: 0 2.5rem;
  padding-top: 1rem;
}

/* Tab content container - takes remaining space and scrolls */
.main-content .tab-content {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  min-height: 0;
}

/* Each tab pane should fill available space and scroll independently */
.main-content .tab-pane {
  height: 100%;
  overflow-y: auto;
  overflow-x: hidden;
  padding: 1.5rem 2.5rem 2rem 2.5rem;
  min-height: 0;
}

/* Content preview - let it flow naturally within scrolling tab pane */
.content-preview {
  white-space: pre-wrap;
  word-wrap: break-word;
}

/* Fragment list styles */
.fragment-list {
  width: 100%;
}

.fragment-list .fragment-item:hover {
  background-color: var(--bs-secondary-bg);
}

/* Tag filtering styles */
.badge[style*="cursor: pointer"]:hover {
  text-decoration: underline;
}
</style>
