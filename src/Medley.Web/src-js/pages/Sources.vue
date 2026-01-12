<template>
    <vertical-menu 
      :display-name="userDisplayName"
      :is-authenticated="userIsAuthenticated"
      :is-open="openSidebarOnMobile"
    />

    <!-- Left Sidebar (List) -->
    <div class="sidebar left-sidebar" :class="{ 'show': openSidebarOnMobile }">
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
              <json-viewer ref="jsonViewer"></json-viewer>
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

<script>
import infiniteScrollMixin from '../mixins/infiniteScroll.js';
import { api, createSignalRConnection } from '@/utils/api.js';
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
} from '@/utils/helpers.js';
import { getUrlParam, setUrlParam, setupPopStateHandler } from '@/utils/url.js';

export default {
  name: 'Sources',
  mixins: [infiniteScrollMixin],
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
      tagging: false,
      detachPopState: null,
      activeTagFilter: null,
      // User info from server
      userDisplayName: window.MedleyUser?.displayName || 'User',
      userIsAuthenticated: window.MedleyUser?.isAuthenticated || false,
      openSidebarOnMobile: window.MedleyUser?.openSidebarOnMobile || false
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
      this.resetPagination();
      this.loading = true;
      this.error = null;
      try {
        let url = `/api/sources?skip=0&take=${this.pagination.pageSize}`;
        if (this.activeTagFilter) {
          url += `&tagTypeId=${this.activeTagFilter.tagTypeId}&value=${encodeURIComponent(this.activeTagFilter.value)}`;
        }
        const sources = await api.get(url);
        this.sources = sources;
        this.updateHasMore(sources);
      } catch (err) {
        this.error = 'Failed to load sources: ' + err.message;
        console.error('Error loading sources:', err);
      } finally {
        this.loading = false;
      }
    },

    async loadMoreItems() {
      const skip = this.pagination.page * this.pagination.pageSize;
      try {
        let url = `/api/sources?skip=${skip}&take=${this.pagination.pageSize}`;
        
        const query = this.searchQuery.trim();
        if (query.length >= 2) {
          url += `&query=${encodeURIComponent(query)}`;
        }
        
        if (this.activeTagFilter) {
          url += `&tagTypeId=${this.activeTagFilter.tagTypeId}&value=${encodeURIComponent(this.activeTagFilter.value)}`;
        }
        
        const sources = await api.get(url);
        this.sources.push(...sources);
        return sources;
      } catch (err) {
        console.error('Error loading more sources:', err);
        throw err;
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
        const sourceIndex = this.sources.findIndex(s => s.id === source.id);
        if (sourceIndex !== -1 && this.selectedSource.tags) {
          this.sources[sourceIndex].tags = this.selectedSource.tags;
        }
      } catch (err) {
        console.error('Error loading source:', err);
        this.selectedSource = null;
      }
    },

    async onSearchInput() {
      const query = this.searchQuery.trim();
      if (query.length >= 2) {
        this.resetPagination();
        try {
          let url = `/api/sources?query=${encodeURIComponent(query)}&skip=0&take=${this.pagination.pageSize}`;
          if (this.activeTagFilter) {
            url += `&tagTypeId=${this.activeTagFilter.tagTypeId}&value=${encodeURIComponent(this.activeTagFilter.value)}`;
          }
          const sources = await api.get(url);
          this.sources = sources;
          this.updateHasMore(sources);
        } catch (err) {
          console.error('Search error:', err);
        }
      } else if (query.length === 0) {
        await this.loadSources();
      }
    },

    async filterByTag(tag) {
      if (this.activeTagFilter && 
          this.activeTagFilter.tagTypeId === tag.tagTypeId && 
          this.activeTagFilter.value === tag.value) {
        await this.clearTagFilter();
      } else {
        this.activeTagFilter = {
          tagTypeId: tag.tagTypeId,
          value: tag.value,
          tagType: tag.tagType
        };
        this.searchQuery = '';
        await this.loadSources();
      }
    },

    async clearTagFilter() {
      this.activeTagFilter = null;
      if (this.searchQuery.trim().length >= 2) {
        await this.onSearchInput();
      } else {
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
    },

    closeFragmentModal() {
      this.selectedFragment = null;
    },

    getFragmentCategoryIcon(category) {
      return getFragmentCategoryIcon(category);
    },

    getIconClass(icon) {
      return getIconClass(icon);
    },

    getSourceTypeIcon(type) {
      return getSourceTypeIcon(type);
    },

    getConfidenceIcon(confidence) {
      return getConfidenceIcon(confidence);
    },

    getConfidenceColor(confidence) {
      return getConfidenceColor(confidence);
    },

    formatDate(date) {
      return formatDate(date);
    }
  },

  async mounted() {
    this.markdownRenderer = initializeMarkdownRenderer();

    // Preload article types for icon display
    getArticleTypes();
    
    await this.loadSources();

    // Setup infinite scroll
    this.setupInfiniteScroll('.sidebar-content');

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
    if (this.signalRConnection) {
      this.signalRConnection.stop();
    }
    if (this.detachPopState) {
      this.detachPopState();
    }
  }
};
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
