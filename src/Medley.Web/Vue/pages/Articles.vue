<template>
    <vertical-menu 
      :display-name="userDisplayName"
      :is-authenticated="userIsAuthenticated"
    />

    <!-- Left Sidebar (List/Tree) -->
    <div class="sidebar left-sidebar" :class="{ 'show': leftSidebarVisible }">
      <div class="sidebar-header">
        <div class="d-flex align-items-center gap-2">
          <h6 class="sidebar-title mb-0 flex-grow-1">Articles</h6>
          <div class="btn-group" role="group" v-cloak>
            <button type="button" class="btn btn-sm btn-outline-secondary" :class="{ active: viewMode === 'tree' }" @click="setViewMode('tree')" title="Tree View">
              <i class="bi bi-diagram-3"></i>
            </button>
            <button type="button" class="btn btn-sm btn-outline-secondary" :class="{ active: viewMode === 'list' }" @click="setViewMode('list')" title="List View">
              <i class="bi bi-list-ul"></i>
            </button>
            <button type="button" class="btn btn-sm btn-outline-secondary position-relative" :class="{ active: viewMode === 'mywork' }" @click="setViewMode('mywork')" title="My Work">
              <i class="bi bi-person"></i>
              <span v-if="myWorkCount > 0" class="position-absolute top-0 start-100 translate-middle badge rounded-pill bg-danger">
                {{ myWorkCount }}
              </span>
            </button>
          </div>
          <button class="btn btn-sm btn-outline-secondary position-relative" @click="showFilterModal" title="Filter Articles" v-cloak>
            <i class="bi bi-funnel"></i>
            <span v-if="hasActiveFilters" class="position-absolute top-0 start-100 translate-middle badge rounded-pill bg-primary">
              {{ activeFilterCount }}
            </span>
          </button>
          <div class="position-relative">
            <button class="btn btn-sm btn-outline-secondary" @click.stop="toggleSidebarMenu" title="Actions">
              <i class="bi bi-three-dots"></i>
            </button>
            <div v-if="ui.sidebarMenuOpen" v-cloak class="dropdown-menu show position-absolute" style="right: 0; top: 100%; margin-top: 0.25rem;" @click.stop>
              <button v-if="viewMode === 'tree'" class="dropdown-item" @click="showCreateArticleModal(null)">New Article</button>
              <span v-else class="dropdown-item-text text-muted fst-italic text-nowrap">No actions available</span>
            </div>
          </div>
        </div>
      </div>
      <div class="sidebar-content">
        <div v-if="ui.loading" class="loading-spinner">
          <div class="spinner-border spinner-border-sm" role="status">
            <span class="visually-hidden">Loading...</span>
          </div>
        </div>
        <div v-else-if="ui.error" class="alert alert-danger" v-cloak>
          {{ ui.error }}
        </div>
        <template v-else>
          <article-tree 
            v-show="viewMode === 'tree'"
            v-cloak
            :articles="articles.list"
            :selected-id="articles.selectedId"
            :expanded-ids="articles.expandedIds"
            :article-type-icon-map="articles.typeIconMap"
            :article-types="articles.types"
            @select="selectArticle"
            @toggle-expand="toggleExpand"
            @create-child="showCreateArticleModal"
            @edit-article="showEditArticleModal"
            @move-article="moveArticle"
          ></article-tree>
          <article-list
            v-show="viewMode === 'list'"
            v-cloak
            :articles="articles.list"
            :selected-id="articles.selectedId"
            :article-type-icon-map="articles.typeIconMap"
            :article-types="articles.types"
            @select="selectArticle"
            @edit-article="showEditArticleModal"
          ></article-list>
          <my-work-list
            v-show="viewMode === 'mywork'"
            v-cloak
            :articles="articles.list"
            :selected-id="articles.selectedId"
            :article-type-icon-map="articles.typeIconMap"
            :article-types="articles.types"
            :current-user-id="currentUserId"
            @select="selectArticle"
            @edit-article="showEditArticleModal"
          ></my-work-list>
        </template>
      </div>
    </div>

    <!-- Main Content -->
    <div class="main-content d-flex flex-column" :style="articles.selected ? 'padding: 0;' : ''">
      <div v-if="!articles.selected" class="empty-state" v-cloak>
        <div class="empty-state-icon">
          <i class="bi bi-file-text"></i>
        </div>
        <div class="empty-state-title">No Article Selected</div>
        <div class="empty-state-text">Select an article from the sidebar to view its details</div>
      </div>
      <template v-else v-cloak>
        <!-- Content Tabs (only show if more than Editor tab exists) -->
        <div v-if="availableTabs.length > 1" class="content-tabs-container">
          <ul class="nav nav-tabs">
            <li v-for="tab in availableTabs" :key="tab.id" class="nav-item">
              <button
                class="nav-link"
                :class="{ 'active': contentTabs.activeTabId === tab.id }"
                type="button"
                @click="switchContentTab(tab.id)">
                {{ tab.label }}
                <i
                  v-if="tab.closeable"
                  class="bi bi-x-lg tab-close-icon"
                  @click.stop="closeContentTab(tab.id)"
                  aria-label="Close"></i>
              </button>
            </li>
          </ul>
        </div>

        <!-- Tab Content -->
        <div class="content-tab-panes flex-grow-1 position-relative">
          <!-- Editor Tab -->
          <div v-show="contentTabs.activeTabId === 'editor'" class="content-tab-pane">
            <tiptap-editor
              ref="tiptapEditor"
              v-model="editor.content"
              :key="articles.selectedId"
              :is-saving="editor.isSaving"
              :auto-save="true"
              :show-save-button="false"
              @save="saveArticle"
              class="flex-grow-1"
              placeholder="Start writing your article content..." />
          </div>

          <!-- Version Tab (single, reused) -->
          <div
            v-if="contentTabs.versionData"
            v-show="contentTabs.activeTabId === 'version'"
            class="content-tab-pane">
            <version-viewer
              :article-id="articles.selectedId"
              :version-id="contentTabs.versionData.versionId"
              @version-changed="handleVersionChanged"
              @version-accepted="handleVersionAccepted"
              @version-rejected="handleVersionRejected" />
          </div>

          <!-- Plan Tab (single, reused) -->
          <div
            v-if="contentTabs.planData"
            v-show="contentTabs.activeTabId === 'plan'"
            class="content-tab-pane">
            <plan-viewer
              :plan-id="contentTabs.planData.planId"
              :article-id="articles.selectedId"
              @conversation-created="handlePlanConversationCreated"
              @close-plan="closeContentTab('plan')" />
          </div>
        </div>
      </template>
    </div>

    <!-- Right Sidebar -->
    <div class="sidebar right-sidebar" :class="{ 'show': rightSidebarVisible }">
      <div class="sidebar-header">
        <div class="sidebar-tabs">
          <button 
            class="sidebar-tab" 
            :class="{ 'active': ui.activeRightTab === 'assistant' }"
            @click="setActiveRightTab('assistant')">
            <i class="bi bi-chat-dots"></i> Assistant
          </button>
          <button 
            class="sidebar-tab" 
            :class="{ 'active': ui.activeRightTab === 'versions' }"
            @click="setActiveRightTab('versions')">
            <i class="bi bi-clock-history"></i> Versions
          </button>
        </div>
      </div>
      <div class="sidebar-tab-content">
        <div v-show="ui.activeRightTab === 'assistant'" class="sidebar-tab-pane">
          <chat-panel 
            ref="chatPanel"
            :article-id="articles.selectedId" 
            :connection="signalr.connection"
            @open-plan="openPlanTab"
            @open-fragment="handleOpenFragment"
            @open-version="handleOpenVersion" />
        </div>
        <div v-show="ui.activeRightTab === 'versions'" class="sidebar-tab-pane">
          <versions-panel 
            ref="versionsPanel"
            :article-id="articles.selectedId"
            :selected-version-id="version.selected?.id"
            @select-version="handleVersionSelect" />
        </div>
      </div>
    </div>

    <!-- Create Article Modal -->
    <div v-if="createModal.visible" v-cloak class="modal d-block" tabindex="-1" style="background-color: rgba(0, 0, 0, 0.5);" @click.self="closeCreateModal">
      <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">New Article</h5>
            <button type="button" class="btn-close" @click="closeCreateModal" aria-label="Close"></button>
          </div>
          <div class="modal-body">
            <div class="mb-3">
              <p class="text-muted small mb-3" v-if="createModal.parentId === null">
                Creating at root level
              </p>
              <p class="text-muted small mb-3" v-else>
                Creating as child article
              </p>
            </div>
            
            <div class="mb-3 form-group-overlap">
              <input 
                type="text" 
                class="form-control" 
                v-model="createModal.title"
                @keyup.enter="createArticle"
                @keyup.esc="closeCreateModal"
                ref="titleInput" />
              <label>Title *</label>
            </div>

            <div class="mb-3 form-group-overlap">
              <select class="form-select" v-model="createModal.typeId">
                <option :value="null" disabled>Select article type</option>
                <option v-for="type in articles.types" :key="type.id" :value="type.id">
                  {{ type.name }}
                </option>
              </select>
              <label>Article Type *</label>
            </div>
          </div>
          <div class="modal-footer">
            <button type="button" class="btn btn-outline-secondary" @click="closeCreateModal">Cancel</button>
            <button type="button" class="btn btn-primary" @click="createArticle" :disabled="createModal.isSubmitting">
              <i class="bi bi-save"></i> Save Article
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Edit Article Modal -->
    <div v-if="editModal.visible" v-cloak class="modal d-block" tabindex="-1" style="background-color: rgba(0, 0, 0, 0.5);" @click.self="closeEditModal">
      <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">Edit Article</h5>
            <button type="button" class="btn-close" @click="closeEditModal" aria-label="Close"></button>
          </div>
          <div class="modal-body">
            <div class="mb-3 form-group-overlap">
              <input 
                type="text" 
                class="form-control" 
                v-model="editModal.title"
                @keyup.enter="updateArticle"
                @keyup.esc="closeEditModal"
                ref="editTitleInput" />
              <label>Title *</label>
            </div>

            <div class="mb-3 form-group-overlap">
              <select class="form-select" v-model="editModal.typeId">
                <option :value="null" disabled>Select article type</option>
                <option v-for="type in articles.types" :key="type.id" :value="type.id">
                  {{ type.name }}
                </option>
              </select>
              <label>Article Type *</label>
            </div>
          </div>
          <div class="modal-footer">
            <button type="button" class="btn btn-outline-secondary" @click="closeEditModal">Cancel</button>
            <button type="button" class="btn btn-primary" @click="updateArticle" :disabled="editModal.isSubmitting">
              <i class="bi bi-save"></i> Save Article
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Filter Modal -->
    <div v-if="filterModal.visible" v-cloak class="modal d-block" tabindex="-1" style="background-color: rgba(0, 0, 0, 0.5);" @click.self="closeFilterModal">
      <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">Filter Articles</h5>
            <button type="button" class="btn-close" @click="closeFilterModal" aria-label="Close"></button>
          </div>
          <div class="modal-body">
            <!-- Text Search -->
            <div class="mb-3 form-group-overlap">
              <input 
                type="text" 
                class="form-control" 
                v-model="filters.query"
                @keyup.enter="applyFilters"
                @keyup.esc="closeFilterModal"
                ref="filterSearchInput"
                placeholder=" " />
              <label>Search by title</label>
            </div>

            <!-- Status Filter -->
            <div class="mb-3">
              <label class="form-label fw-semibold">Status</label>
              <div class="row g-2">
                <div class="col-6">
                  <div class="form-check">
                    <input class="form-check-input" type="checkbox" :checked="isStatusSelected(0)" @change="toggleStatusFilter(0)" id="status-draft">
                    <label class="form-check-label" for="status-draft">
                      <i class="bi bi-pencil me-1 text-secondary"></i>Draft
                    </label>
                  </div>
                </div>
                <div class="col-6">
                  <div class="form-check">
                    <input class="form-check-input" type="checkbox" :checked="isStatusSelected(1)" @change="toggleStatusFilter(1)" id="status-review">
                    <label class="form-check-label" for="status-review">
                      <i class="bi bi-eye me-1 text-info"></i>Review
                    </label>
                  </div>
                </div>
                <div class="col-6">
                  <div class="form-check">
                    <input class="form-check-input" type="checkbox" :checked="isStatusSelected(2)" @change="toggleStatusFilter(2)" id="status-approved">
                    <label class="form-check-label" for="status-approved">
                      <i class="bi bi-check-circle me-1 text-success"></i>Approved
                    </label>
                  </div>
                </div>
                <div class="col-6">
                  <div class="form-check">
                    <input class="form-check-input" type="checkbox" :checked="isStatusSelected(3)" @change="toggleStatusFilter(3)" id="status-archived">
                    <label class="form-check-label" for="status-archived">
                      <i class="bi bi-archive me-1 text-danger"></i>Archived
                    </label>
                  </div>
                </div>
              </div>
            </div>

            <!-- Article Type Filter -->
            <div class="mb-3">
              <label class="form-label fw-semibold">Article Type</label>
              <div class="row g-2">
                <div v-for="type in articles.types" :key="type.id" class="col-6">
                  <div class="form-check">
                    <input class="form-check-input" type="checkbox" :checked="isArticleTypeSelected(type.id)" @change="toggleArticleTypeFilter(type.id)" :id="'type-' + type.id">
                    <label class="form-check-label" :for="'type-' + type.id">
                      <i :class="getArticleTypeIconClass(type.icon)" class="me-1"></i>{{ type.name }}
                    </label>
                  </div>
                </div>
              </div>
            </div>
          </div>
          <div class="modal-footer">
            <button type="button" class="btn btn-outline-secondary" @click="clearFilters">
              <i class="bi bi-x-circle"></i> Clear Filters
            </button>
            <button type="button" class="btn btn-primary" @click="applyFilters">
              <i class="bi bi-check-circle"></i> Apply Filters
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Fragment Modal -->
    <fragment-modal
      :fragment="selectedFragment"
      :visible="!!selectedFragment"
      @close="closeFragmentModal" />
</template>

<script>
import { api, createSignalRConnection } from '@/utils/api.js';
import { 
  getStatusBadgeClass,
  getStatusIcon,
  getStatusColorClass,
  formatDate,
  getArticleTypes,
  showProcessingSpinner,
  showUserTurnIndicator,
  showToast
} from '@/utils/helpers.js';
import { getUrlParam } from '@/utils/url.js';

import FragmentModal from '../components/FragmentModal.vue';
import VersionViewer from '../components/VersionViewer.vue';
import MyWorkList from '../components/MyWorkList.vue';
import articleModalMixin from '../mixins/articleModal.js';
import articleVersionMixin from '../mixins/articleVersion.js';
import articleSignalRMixin from '../mixins/articleSignalR.js';
import articleFilterMixin from '../mixins/articleFilter.js';
import { useSidebarState } from '@/composables/useSidebarState'
import { useArticleTree } from '@/composables/useArticleTree'
import { useMyWork } from '@/composables/useMyWork'
import { useArticleVersions } from '@/composables/useArticleVersions'
import { useRouter } from 'vue-router'
import { computed } from 'vue'

export default {
  name: 'Articles',
  components: {
    FragmentModal,
    VersionViewer,
    MyWorkList
  },
  mixins: [
    articleModalMixin,
    articleVersionMixin,
    articleSignalRMixin,
    articleFilterMixin
  ],
  setup() {
    const { leftSidebarVisible, rightSidebarVisible } = useSidebarState()
    const router = useRouter()
    return { leftSidebarVisible, rightSidebarVisible, router }
  },
  data() {
    return {
      // Article state
      articles: {
        list: [],
        selected: null,
        selectedId: null,
        types: [],
        typeIconMap: {},
        typeIndexMap: {},
        expandedIds: new Set(),
        index: new Map(),
        parentPathCache: new Map()
      },

      // View mode state
      viewMode: 'tree',

      // Editor state
      editor: {
        title: '',
        content: '',
        isSaving: false
      },

      // Content tabs state
      contentTabs: {
        activeTabId: 'editor',
        versionData: null,
        planData: null
      },

      // Create modal state
      createModal: {
        visible: false,
        title: '',
        typeId: null,
        parentId: null,
        isSubmitting: false
      },

      // Edit modal state
      editModal: {
        visible: false,
        articleId: null,
        title: '',
        typeId: null,
        isSubmitting: false
      },

      // UI state
      ui: {
        loading: false,
        error: null,
        sidebarMenuOpen: false,
        activeRightTab: 'assistant'
      },

      // Version state
      version: {
        selected: null,
        diffHtml: null,
        loadingDiff: false,
        diffError: null
      },

      // SignalR state
      signalr: {
        connection: null,
        updateQueue: [],
        processing: false
      },

      // Drag state
      dragState: {
        draggingArticleId: null,
        dragOverId: null
      },

      // User info from server
      userDisplayName: window.MedleyUser?.displayName || 'User',
      userIsAuthenticated: window.MedleyUser?.isAuthenticated || false,
      currentUserId: window.MedleyUser?.id || null,

      // Fragment modal state
      selectedFragment: null
    };
  },
  provide() {
    return {
      dragState: this.dragState
    };
  },
  computed: {
    hasUnsavedChanges() {
      return this.$refs.tiptapEditor?.hasChanges || false;
    },
    myWorkCount() {
      // Use shared My Work composable to get count
      const { myWorkCount } = useMyWork(
        computed(() => this.articles.list),
        computed(() => this.currentUserId)
      );
      return myWorkCount.value;
    },
    availableTabs() {
      const tabs = [{ id: 'editor', label: 'Editor', closeable: false, type: 'editor' }];

      if (this.contentTabs.versionData) {
        tabs.push({
          id: 'version',
          label: `Version ${this.contentTabs.versionData.versionNumber}`,
          closeable: true,
          type: 'version'
        });
      }

      if (this.contentTabs.planData) {
        tabs.push({
          id: 'plan',
          label: 'Plan',
          closeable: true,
          type: 'plan'
        });
      }

      return tabs;
    }
  },
  methods: {
    async loadArticles() {
      this.ui.loading = true;
      this.ui.error = null;
      try {
        const queryString = this.buildFilterQueryString();
        this.articles.list = await api.get(`/api/articles/tree${queryString}`);
        
        // Initialize article tree composable with current state
        const treeOps = useArticleTree(this);
        treeOps.buildArticleIndex();
        treeOps.buildParentPathCache();
      } catch (err) {
        this.ui.error = 'Failed to load articles: ' + err.message;
        console.error('Error loading articles:', err);
      } finally {
        this.ui.loading = false;
      }
    },

    async loadArticleTypes() {
      try {
        this.articles.types = await getArticleTypes();

        this.articles.typeIconMap = {};
        this.articles.typeIndexMap = {};
        this.articles.types.forEach(type => {
          this.articles.typeIconMap[type.id] = type.icon || 'bi-file-text';
          this.articles.typeIndexMap[type.id] = type;
        });
      } catch (err) {
        console.error('Error loading article types:', err);
        showToast('error', 'Failed to load article types');
      }
    },

    async selectArticle(article, replaceState = false) {
      if (article.id === this.articles.selectedId) {
        return;
      }

      if (this.hasUnsavedChanges) {
        const shouldProceed = await this.promptUnsavedChanges();
        if (!shouldProceed) {
          return;
        }
      }

      try {
        const fullArticle = await api.get(`/api/articles/${article.id}`);

        if (this.articles.selectedId && this.signalr.connection && this.signalr.connection.state === signalR.HubConnectionState.Connected) {
          await this.signalr.connection.invoke('LeaveArticle', this.articles.selectedId);
        }

        this.editor.title = fullArticle.title;
        this.editor.content = fullArticle.content || '';
        this.articles.selected = fullArticle;
        this.articles.selectedId = article.id;

        if (this.signalr.connection && this.signalr.connection.state === signalR.HubConnectionState.Connected) {
          await this.signalr.connection.invoke('JoinArticle', article.id);
        }

        this.clearVersionSelection();
        this.clearAllTabs();
        // Load plan and AI version - load AI version last so it takes priority if both exist
        await this.loadDraftPlan(article.id);
        await this.loadLatestAIVersion(article.id);
        this.expandParents(article.id);

        // Use Vue Router to update the URL, which will trigger the App.vue watcher
        if (replaceState) {
          await this.router.replace({ query: { id: article.id } });
        } else {
          await this.router.push({ query: { id: article.id } });
        }
      } catch (err) {
        console.error('Error loading article:', err);
        this.articles.selected = null;
      }
    },


    toggleExpand(articleId) {
      if (this.articles.expandedIds.has(articleId)) {
        this.articles.expandedIds.delete(articleId);
      } else {
        this.articles.expandedIds.add(articleId);
      }
    },

    expandParents(articleId) {
      const treeOps = useArticleTree(this);
      const parents = treeOps.getArticleParents(articleId);
      if (parents) {
        parents.forEach(parentId => {
          this.articles.expandedIds.add(parentId);
        });
      }
    },

    insertArticleIntoTree(article) {
      const treeOps = useArticleTree(this);
      treeOps.insertArticleIntoTree(article);
    },

    updateArticleInTree(articleId, updates) {
      const treeOps = useArticleTree(this);
      treeOps.updateArticleInTree(articleId, updates);
    },

    removeArticleFromTree(articleId) {
      const treeOps = useArticleTree(this);
      treeOps.removeArticleFromTree(articleId);
    },

    moveArticleInTree(articleId, oldParentId, newParentId) {
      const treeOps = useArticleTree(this);
      treeOps.moveArticleInTree(articleId, oldParentId, newParentId);
    },


    async saveArticle(retryCount = 0) {
      if (!this.articles.selected) return;

      this.editor.isSaving = true;
      try {
        const response = await api.put(`/api/articles/${this.articles.selected.id}/content`, {
          content: this.editor.content
        });

        if (response && response.versionNumber) {
          console.log(`Article saved - Version ${response.versionNumber} (${response.isNewVersion ? 'new' : 'updated'})`);
        }
      } catch (err) {
        console.error('Error saving article:', err);

        if (retryCount < 3) {
          const delay = Math.pow(2, retryCount) * 1000;
          console.log(`Retrying save in ${delay}ms (attempt ${retryCount + 1}/3)...`);
          await new Promise(resolve => setTimeout(resolve, delay));
          return this.saveArticle(retryCount + 1);
        }

        console.error('Failed to save article after 3 attempts');
      } finally {
        this.editor.isSaving = false;
      }
    },

    deleteArticle() {
      bootbox.confirm({
        title: 'Delete Article',
        message: 'Are you sure you want to delete this article?',
        buttons: {
          confirm: {
            label: 'Delete',
            className: 'btn-danger'
          },
          cancel: {
            label: 'Cancel',
            className: 'btn-secondary'
          }
        },
        callback: (confirmed) => {
          if (confirmed) {
            bootbox.alert({
              title: 'Delete Article',
              message: 'Delete functionality coming soon',
              className: 'bootbox-info'
            });
          }
        }
      });
    },

    toggleSidebarMenu() {
      this.ui.sidebarMenuOpen = !this.ui.sidebarMenuOpen;
    },

    setActiveRightTab(tab) {
      this.ui.activeRightTab = tab;
    },

    formatDate(date) {
      return formatDate(date);
    },

    getStatusBadgeClass(status) {
      return getStatusBadgeClass(status);
    },

    setViewMode(mode) {
      this.viewMode = mode;
      localStorage.setItem('articlesViewMode', mode);
    },

    toggleViewMode() {
      const newMode = this.viewMode === 'tree' ? 'list' : 'tree';
      this.setViewMode(newMode);
    },

    promptUnsavedChanges() {
      return new Promise((resolve) => {
        bootbox.dialog({
          title: 'Unsaved Changes',
          message: 'You have unsaved changes. What would you like to do?',
          buttons: {
            save: {
              label: '<i class="bi bi-save"></i> Save Changes',
              className: 'btn-primary',
              callback: async () => {
                await this.saveArticle();
                resolve(true);
              }
            },
            discard: {
              label: 'Discard Changes',
              className: 'btn-outline-danger',
              callback: () => {
                resolve(true);
              }
            },
            cancel: {
              label: 'Cancel',
              className: 'btn-secondary',
              callback: () => {
                resolve(false);
              }
            }
          }
        });
      });
    },

    async moveArticle(sourceArticleId, targetParentId) {
      const sourceArticle = this.articles.index.get(sourceArticleId);
      const targetParent = this.articles.index.get(targetParentId);

      if (!sourceArticle || !targetParent) {
        console.error('Source or target article not found');
        bootbox.alert({
          title: 'Move Article',
          message: 'Could not find source or target article',
          className: 'bootbox-error'
        });
        return;
      }

      bootbox.confirm({
        title: 'Move Article',
        message: `Move <strong>${sourceArticle.title}</strong> under <strong>${targetParent.title}</strong>?`,
        buttons: {
          confirm: {
            label: 'Move',
            className: 'btn-primary'
          },
          cancel: {
            label: 'Cancel',
            className: 'btn-secondary'
          }
        },
        callback: async (confirmed) => {
          if (confirmed) {
            try {
              await api.put(`/api/articles/${sourceArticleId}/move`, {
                newParentArticleId: targetParentId
              });
            } catch (err) {
              bootbox.alert({
                title: 'Move Article',
                message: `Failed to move article: ${err.message}`,
                className: 'bootbox-error'
              });
              console.error('Error moving article:', err);
            }
          }
        }
      });
    },


    switchContentTab(tabId) {
      this.contentTabs.activeTabId = tabId;
    },

    closeContentTab(tabId) {
      if (tabId === 'version') {
        this.contentTabs.versionData = null;
        this.contentTabs.activeTabId = 'editor';
      } else if (tabId === 'plan') {
        this.contentTabs.planData = null;
        this.contentTabs.activeTabId = 'editor';
      }
    },

    openPlanTab(planId) {
      this.contentTabs.planData = { planId };
      this.contentTabs.activeTabId = 'plan';
    },

    async openVersionTab(version) {
      // Load version details if we only have the ID
      let versionDetails = version;
      if (typeof version === 'string') {
        try {
          versionDetails = await api.get(`/api/articles/${this.articles.selectedId}/versions/${version}`);
        } catch (err) {
          console.error('Error loading version details:', err);
          showToast('error', 'Failed to load version');
          return;
        }
      }

      this.contentTabs.versionData = {
        versionId: versionDetails.id,
        versionNumber: versionDetails.versionNumber
      };

      this.contentTabs.activeTabId = 'version';
    },

    async handleVersionChanged(newVersionId) {
      // Update the tab data with the new version
      await this.openVersionTab(newVersionId);
    },

    async handleVersionAccepted(response) {
      // Close the version tab
      this.closeContentTab('version');
      
      // Reload the article to get the latest content
      if (this.articles.selectedId) {
        try {
          const fullArticle = await api.get(`/api/articles/${this.articles.selectedId}`);
          this.editor.content = fullArticle.content || '';
          this.articles.selected = fullArticle;
          
          showToast('success', 'Version accepted successfully');
        } catch (err) {
          console.error('Error reloading article:', err);
          showToast('error', 'Failed to reload article');
        }
      }
    },

    async handleVersionRejected(versionId) {
      // Close the version tab
      this.closeContentTab('version');
      
      showToast('success', 'Version rejected');
    },

    async loadDraftPlan(articleId) {
      try {
        const response = await api.get(`/api/articles/${articleId}/plans/active`);

        if (response && response.id) {
          this.openPlanTab(response.id);
        }
      } catch (err) {
        if (err.response && err.response.status === 204) {
          return;
        }
        console.error('Error loading draft plan:', err);
      }
    },

    async loadLatestAIVersion(articleId) {
      // Use composable to find pending AI version by status
      const versionState = useArticleVersions(computed(() => articleId));
      
      // Wait for versions to be loaded if not already
      if (!versionState.loaded.value) {
        // Versions will be loaded by VersionsPanel watch handler
        // Check again after a tick
        this.$nextTick(() => {
          if (versionState.pendingAiVersion.value) {
            this.openVersionTab(versionState.pendingAiVersion.value);
          }
        });
        return;
      }

      // Use cached data - pendingAiVersion is found by status filter
      if (versionState.pendingAiVersion.value) {
        this.openVersionTab(versionState.pendingAiVersion.value);
      }
    },

    clearAllTabs() {
      this.contentTabs.versionData = null;
      this.contentTabs.planData = null;
      this.contentTabs.activeTabId = 'editor';
    },

    async handleOpenFragment(fragmentId) {
      if (!fragmentId) return;

      try {
        const fragment = await api.get(`/api/fragments/${fragmentId}`);
        this.selectedFragment = fragment;
      } catch (err) {
        console.error('Error loading fragment:', err);
        showToast('error', 'Failed to load fragment');
      }
    },

    async handleOpenVersion(versionId) {
      if (!versionId) return;

      try {
        const version = await api.get(`/api/articles/${this.articles.selectedId}/versions/${versionId}`);
        this.openVersionTab(version);
      } catch (err) {
        console.error('Error loading version:', err);
        showToast('error', 'Failed to load version');
      }
    },

    closeFragmentModal() {
      this.selectedFragment = null;
    },

    async handlePlanConversationCreated(conversationId) {
      // Switch to the assistant tab
      this.setActiveRightTab('assistant');
      
      // Load the new conversation in the chat panel
      await this.$nextTick();
      if (this.$refs.chatPanel) {
        await this.$refs.chatPanel.loadConversation(conversationId);
      }
    }
  },

  async mounted() {
    const savedViewMode = localStorage.getItem('articlesViewMode');
    if (savedViewMode && (savedViewMode === 'tree' || savedViewMode === 'list' || savedViewMode === 'mywork')) {
      this.viewMode = savedViewMode;
    }

    await this.initializeSignalRConnection();

    await Promise.all([
      this.loadArticleTypes(),
      this.loadArticles()
    ]);

    const treeOps = useArticleTree(this);
    treeOps.sortArticlesRecursive(this.articles.list);

    const articleIdFromUrl = getUrlParam('id');
    if (articleIdFromUrl) {
      const article = this.articles.index.get(articleIdFromUrl);
      if (article) {
        await this.selectArticle(article, true);
      }
    }

    document.addEventListener('click', () => {
      this.ui.sidebarMenuOpen = false;
    });

    this.handleBeforeUnload = (event) => {
      if (this.hasUnsavedChanges) {
        event.preventDefault();
        event.returnValue = 'You have unsaved changes. Are you sure you want to leave?';
        return event.returnValue;
      }
    };
    window.addEventListener('beforeunload', this.handleBeforeUnload);
  },

  beforeUnmount() {
    if (this.handleBeforeUnload) {
      window.removeEventListener('beforeunload', this.handleBeforeUnload);
    }

    this.disconnectSignalR();
  }
};
</script>
