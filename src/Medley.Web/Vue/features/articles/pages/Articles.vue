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
          <div class="dropdown-container position-relative">
            <button 
              class="btn btn-sm btn-outline-secondary" 
              @click="toggleDropdown($event, 'header-actions')"
              title="Actions">
              <i class="bi bi-three-dots"></i>
            </button>
            <ul v-if="isDropdownOpen('header-actions')" class="dropdown-menu dropdown-menu-end show" :class="getPositionClasses()">
              <li v-if="viewMode === 'tree'">
                <button class="dropdown-item" @click="showCreateArticleModal(null); closeDropdown()">New Article</button>
              </li>
              <li v-else>
                <span class="dropdown-item-text text-muted fst-italic">No actions available</span>
              </li>
            </ul>
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
            @select="selectArticle"
            @edit-article="showEditArticleModal"
          ></article-list>
          <my-work-list
            v-show="viewMode === 'mywork'"
            v-cloak
            :articles="articles.list"
            :selected-id="articles.selectedId"
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
                <option v-for="type in articleTypes" :key="type.id" :value="type.id">
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
                <option v-for="type in articleTypes" :key="type.id" :value="type.id">
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
                <div v-for="type in articleTypes" :key="type.id" class="col-6">
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
      @close="closeFragmentModal"
      @updated="handleFragmentUpdated" />
</template>

<script setup lang="ts">

import { ref, reactive, computed, provide, onMounted, onBeforeUnmount, nextTick, watch, type Ref } from 'vue';
import { useRouter, useRoute } from 'vue-router';

// Define props to handle attributes passed by router
defineProps<{
  id?: string;
}>();
import { HubConnectionState } from '@microsoft/signalr';
import { apiClients } from '@/utils/apiClients';
import type { 
  ArticleDto,
  ArticleSummaryDto,
  ArticleVersionDto,
  FragmentDto,
  ArticleUpdateContentRequest,
  ArticleStatus
} from '@/types/api-client';
import { 
  getStatusBadgeClass,
  formatDate,
  showToast
} from '@/utils/helpers';
import type { ArticleHubConnection } from '../types/article-hub';

// Composables
import { useSidebarState } from '@/composables/useSidebarState';
import { useArticleTree } from '../composables/useArticleTree';
import { useMyWork } from '../composables/useMyWork';
import { useVersionsState } from '../composables/useVersionsState';
import { useArticleModal } from '../composables/useArticleModal';
import { useVersionViewer } from '../composables/useVersionViewer';
import { useArticleSignalR } from '../composables/useArticleSignalR';
import { useArticleFilter } from '../composables/useArticleFilter';
import { useArticleTypes } from '../composables/useArticleTypes';
import { useDropDown } from '@/composables/useDropDown';

// Components
import FragmentModal from '../../sources/components/FragmentModal.vue';
import VersionViewer from '../components/VersionViewer.vue';
import MyWorkList from '../components/MyWorkList.vue';
import ChatPanel from '../components/ChatPanel.vue';
import VersionsPanel from '../components/VersionsPanel.vue';
import ArticleTree from '../components/ArticleTree.vue';
import ArticleList from '../components/ArticleList.vue';
import TiptapEditor from '@/components/TiptapEditor.vue';
import PlanViewer from '../components/PlanViewer.vue';
import VerticalMenu from '@/components/VerticalMenu.vue';

// Global types
declare const bootbox: any;

// ============================================================================
// INTERFACES
// ============================================================================

interface ParentPathItem {
  id: string;
  title: string;
}

interface ArticlesState {
  list: ArticleSummaryDto[];
  selected: ArticleSummaryDto | null;
  selectedId: string | null;
  expandedIds: Set<string>;
  index: Map<string, ArticleDto>;
  parentPathCache: Map<string, ParentPathItem[]>;
  typeIndexMap: Record<string, ArticleTypeDto>;
}

interface EditorState {
  title: string;
  content: string;
  isSaving: boolean;
}

interface ContentTabsState {
  activeTabId: string;
  versionData: { versionId: string; versionNumber: number } | null;
  planData: { planId: string } | null;
}

interface UIState {
  loading: boolean;
  error: string | null;
  activeRightTab: 'assistant' | 'versions';
}

interface VersionState {
  selected: ArticleVersionDto | null;
}

interface SignalRState {
  connection: ArticleHubConnection | null;
}

interface DragState {
  draggingArticleId: string | null;
  dragOverId: string | null;
}

interface ContentTab {
  id: string;
  label: string;
  closeable: boolean;
  type: string;
}

// Component ref types
interface TiptapEditorRef {
  hasChanges: boolean;
  syncHeading: (title: string) => void;
}

interface ChatPanelRef {
  loadConversation: (conversationId: string) => Promise<void>;
}

interface VersionsPanelRef {
  loadVersions: () => Promise<void>;
}

// Window types
interface MedleyUser {
  displayName?: string;
  isAuthenticated?: boolean;
  id?: string;
}

declare global {
  interface Window {
    MedleyUser?: MedleyUser;
  }
}

// ============================================================================
// SETUP
// ============================================================================

const router = useRouter();
const route = useRoute();
const { leftSidebarVisible, rightSidebarVisible } = useSidebarState();

// ============================================================================
// TEMPLATE REFS
// ============================================================================

const tiptapEditor = ref<TiptapEditorRef | null>(null);
const chatPanel = ref<ChatPanelRef | null>(null);
const versionsPanel = ref<VersionsPanelRef | null>(null);
const titleInput = ref<HTMLInputElement | null>(null);
const editTitleInput = ref<HTMLInputElement | null>(null);
const filterSearchInput = ref<HTMLInputElement | null>(null);

// ============================================================================
// STATE
// ============================================================================

// Article state
const articles = reactive<ArticlesState>({
  list: [],
  selected: null,
  selectedId: null,
  expandedIds: new Set(),
  index: new Map(),
  parentPathCache: new Map(),
  typeIndexMap: {}
});

// View mode state
const viewMode = ref<'tree' | 'list' | 'mywork'>('tree');

// Editor state
const editor = reactive<EditorState>({
  title: '',
  content: '',
  isSaving: false
});

// Content tabs state
const contentTabs = reactive<ContentTabsState>({
  activeTabId: 'editor',
  versionData: null,
  planData: null
});

// UI state
const ui = reactive<UIState>({
  loading: false,
  error: null,
  activeRightTab: 'assistant'
});

// Version state
const version = reactive<VersionState>({
  selected: null
});

// SignalR state
const signalr = reactive<SignalRState>({
  connection: null
});

// Drag state
const dragState = reactive<DragState>({
  draggingArticleId: null,
  dragOverId: null
});

// User info from server
const userDisplayName = window.MedleyUser?.displayName ?? 'User';
const userIsAuthenticated = window.MedleyUser?.isAuthenticated ?? false;
const currentUserId = window.MedleyUser?.id ?? null;

// Fragment modal state
const selectedFragment = ref<FragmentDto | null>(null);

// Provide drag state for child components
provide('dragState', dragState);

// ============================================================================
// COMPOSABLES
// ============================================================================

// Dropdown composable
const { toggleDropdown, closeDropdown, isDropdownOpen, getPositionClasses } = useDropDown();

// Article types composable
const { types: articleTypes, typeIndexMap, loadArticleTypes } = useArticleTypes();

// Sync typeIndexMap with articles state
watch(typeIndexMap, (newMap) => {
  articles.typeIndexMap = newMap;
}, { immediate: true });

// My Work composable - call once at top level
const { myWorkCount } = useMyWork(
  computed(() => articles.list),
  computed(() => currentUserId)
);

// Article tree operations composable
const treeOps = useArticleTree({
  articles: articles,
  editor: editor
});

// Article modals composable (create and edit)
const {
  createModal,
  editModal,
  showCreateArticleModal,
  closeCreateModal,
  createArticle,
  showEditArticleModal,
  closeEditModal,
  updateArticle
} = useArticleModal({
  insertArticleIntoTree: (article: ArticleSummaryDto) => treeOps.insertArticleIntoTree(article),
  updateArticleInTree: (articleId: string, updates: Partial<ArticleSummaryDto>) => treeOps.updateArticleInTree(articleId, updates),
  selectArticle: async (article: ArticleSummaryDto, shouldJoinSignalR?: boolean) => {
    await selectArticle(article, !shouldJoinSignalR);
  },
  articlesIndex: articles.index,
  selectedArticleId: computed(() => articles.selectedId),
  titleInputRef: titleInput,
  editTitleInputRef: editTitleInput,
  tiptapEditorRef: tiptapEditor
});

// Filter composable
const {
  filterModal,
  filters,
  hasActiveFilters,
  activeFilterCount,
  showFilterModal,
  closeFilterModal,
  applyFilters,
  clearFilters,
  toggleStatusFilter,
  isStatusSelected,
  toggleArticleTypeFilter,
  isArticleTypeSelected,
  buildFilterQueryString,
  getArticleTypeIconClass
} = useArticleFilter({
  loadArticles: async () => await loadArticles(),
  searchInputRef: filterSearchInput
});

// Version composable
const {
  handleVersionSelect,
  clearVersionSelection
} = useVersionViewer({
  openVersionTab: (version) => openVersionTab(version),
  switchContentTab: (tab) => { contentTabs.activeTabId = tab; },
  selectedArticleId: computed(() => articles.selectedId)
});

// SignalR composable (initialized in onMounted)
let signalRMethods: ReturnType<typeof useArticleSignalR> | null = null;

// ============================================================================
// COMPUTED PROPERTIES
// ============================================================================

const hasUnsavedChanges = computed(() => {
  return tiptapEditor.value?.hasChanges || false;
});

const availableTabs = computed<ContentTab[]>(() => {
  const tabs: ContentTab[] = [{ id: 'editor', label: 'Editor', closeable: false, type: 'editor' }];

  if (contentTabs.versionData) {
    tabs.push({
      id: 'version',
      label: `Version ${contentTabs.versionData.versionNumber}`,
      closeable: true,
      type: 'version'
    });
  }

  if (contentTabs.planData) {
    tabs.push({
      id: 'plan',
      label: 'Plan',
      closeable: true,
      type: 'plan'
    });
  }

  return tabs;
});

// ============================================================================
// METHODS - Article Loading
// ============================================================================

const loadArticles = async (): Promise<void> => {
  ui.loading = true;
  ui.error = null;
  try {
    // Extract filter parameters for NSwag client
    const query = filters.value.query.trim() || undefined;
    const statuses = filters.value.statuses.length > 0 
      ? filters.value.statuses as any as number[] 
      : undefined;
    const articleTypeIds = filters.value.articleTypeIds.length > 0 
      ? filters.value.articleTypeIds 
      : undefined;
    
    articles.list = await apiClients.articles.getTree(query, statuses, articleTypeIds);
    
    // Build indexes and caches - explicitly pass the loaded articles
    treeOps.buildArticleIndex(articles.list);
    treeOps.buildParentPathCache(articles.list);
  } catch (err: any) {
    ui.error = 'Failed to load articles: ' + err.message;
    console.error('Error loading articles:', err);
  } finally {
    ui.loading = false;
  }
};

// ============================================================================
// METHODS - Article Selection and Navigation
// ============================================================================

const selectArticle = async (article: ArticleSummaryDto, replaceState: boolean = false): Promise<void> => {
  if (article.id === articles.selectedId) {
    return;
  }

  if (hasUnsavedChanges.value) {
    const shouldProceed = await promptUnsavedChanges();
    if (!shouldProceed) {
      return;
    }
  }

  try {
    const fullArticle = await apiClients.articles.get(article.id!);

    if (articles.selectedId && signalr.connection && signalr.connection.state === HubConnectionState.Connected) {
      await signalr.connection.invoke('LeaveArticle', articles.selectedId);
    }

    editor.title = fullArticle.title ?? '';
    editor.content = fullArticle.content ?? '';
    articles.selected = fullArticle;
    articles.selectedId = article.id ?? null;

    if (signalr.connection && signalr.connection.state === HubConnectionState.Connected && article.id) {
      await signalr.connection.invoke('JoinArticle', article.id);
    }

    clearVersionSelection();
    clearAllTabs();
    
    // Only proceed if article has an ID
    if (article.id) {
      // Load plan and AI version - load AI version last so it takes priority if both exist
      await loadDraftPlan(article.id);
      await loadLatestAIVersion(article.id);
      expandParents(article.id);
    }

    // Use Vue Router to update the URL
    if (replaceState) {
      await router.replace({ query: { id: article.id } });
    } else {
      await router.push({ query: { id: article.id } });
    }
  } catch (err: any) {
    console.error('Error loading article:', err);
    articles.selected = null;
  }
};

const promptUnsavedChanges = (): Promise<boolean> => {
  return new Promise((resolve) => {
    bootbox.dialog({
      title: 'Unsaved Changes',
      message: 'You have unsaved changes. What would you like to do?',
      buttons: {
        save: {
          label: '<i class="bi bi-save"></i> Save Changes',
          className: 'btn-primary',
          callback: async () => {
            await saveArticle();
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
};

// ============================================================================
// METHODS - Tree Operations
// ============================================================================

const toggleExpand = (articleId: string): void => {
  if (articles.expandedIds.has(articleId)) {
    articles.expandedIds.delete(articleId);
  } else {
    articles.expandedIds.add(articleId);
  }
};

const expandParents = (articleId: string | undefined): void => {
  if (!articleId) return;
  
  const parents = treeOps.getArticleParents(articleId);
  if (parents) {
    parents.forEach(parentId => {
      articles.expandedIds.add(parentId);
    });
  }
};

const insertArticleIntoTree = (article: ArticleSummaryDto): void => {
  treeOps.insertArticleIntoTree(article);
};

const updateArticleInTree = (articleId: string, updates: Partial<ArticleSummaryDto>): void => {
  treeOps.updateArticleInTree(articleId, updates);
};

const removeArticleFromTree = (articleId: string): void => {
  treeOps.removeArticleFromTree(articleId);
};

const moveArticleInTree = (articleId: string, oldParentId: string | null, newParentId: string | null): void => {
  treeOps.moveArticleInTree(articleId, oldParentId, newParentId);
};

const moveArticle = async (sourceArticleId: string, targetParentId: string): Promise<void> => {
  const sourceArticle = articles.index.get(sourceArticleId);
  const targetParent = articles.index.get(targetParentId);

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
    callback: async (confirmed: boolean) => {
      if (confirmed) {
        try {
          await apiClients.articles.move(sourceArticleId, {
            newParentArticleId: targetParentId
          });
        } catch (err: any) {
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
};

// ============================================================================
// METHODS - Article Saving
// ============================================================================

const saveArticle = async (retryCount: number = 0): Promise<void> => {
  if (!articles.selected) return;

  editor.isSaving = true;
  try {
    const response = await apiClients.articles.updateContent(
      articles.selected.id!,
      { content: editor.content }
    );

    if (response && response.versionNumber) {
      console.log(`Article saved - Version ${response.versionNumber} (${response.isNewVersion ? 'new' : 'updated'})`);
    }
  } catch (err: any) {
    console.error('Error saving article:', err);

    if (retryCount < 3) {
      const delay = Math.pow(2, retryCount) * 1000;
      console.log(`Retrying save in ${delay}ms (attempt ${retryCount + 1}/3)...`);
      await new Promise(resolve => setTimeout(resolve, delay));
      return saveArticle(retryCount + 1);
    }

    console.error('Failed to save article after 3 attempts');
  } finally {
    editor.isSaving = false;
  }
};

// ============================================================================
// METHODS - Content Tabs
// ============================================================================

const switchContentTab = (tabId: string): void => {
  contentTabs.activeTabId = tabId;
};

const closeContentTab = (tabId: string): void => {
  if (tabId === 'version') {
    contentTabs.versionData = null;
    contentTabs.activeTabId = 'editor';
  } else if (tabId === 'plan') {
    contentTabs.planData = null;
    contentTabs.activeTabId = 'editor';
  }
};

const openPlanTab = (planId: string): void => {
  contentTabs.planData = { planId };
  contentTabs.activeTabId = 'plan';
};

const openVersionTab = async (version: ArticleVersionDto | string): Promise<void> => {
  // Load version details if we only have the ID
  let versionDetails: ArticleVersionDto;
  if (typeof version === 'string') {
    try {
      versionDetails = await apiClients.articles.getVersion(articles.selectedId!, version);
    } catch (err: any) {
      console.error('Error loading version details:', err);
      showToast('error', 'Failed to load version');
      return;
    }
  } else {
    versionDetails = version;
  }

  contentTabs.versionData = {
    versionId: versionDetails.id ?? '',
    versionNumber: versionDetails.versionNumber ?? 0
  };

  contentTabs.activeTabId = 'version';
};

const clearAllTabs = (): void => {
  contentTabs.versionData = null;
  contentTabs.planData = null;
  contentTabs.activeTabId = 'editor';
};

// ============================================================================
// METHODS - Version Handlers
// ============================================================================

const handleVersionChanged = async (newVersionId: string): Promise<void> => {
  // Update the tab data with the new version
  await openVersionTab(newVersionId);
};

const handleVersionAccepted = async (response: { versionNumber?: number }): Promise<void> => {
  // Close the version tab
  closeContentTab('version');
  
  // Reload the article to get the latest content
  if (articles.selectedId) {
    try {
      const fullArticle = await apiClients.articles.get(articles.selectedId);
      editor.content = fullArticle.content ?? '';
      articles.selected = fullArticle;
      
      showToast('success', 'Version accepted successfully');
    } catch (err: any) {
      console.error('Error reloading article:', err);
      showToast('error', 'Failed to reload article');
    }
  }
};

const handleVersionRejected = async (versionId: string): Promise<void> => {
  // Close the version tab
  closeContentTab('version');
  
  showToast('success', 'Version rejected');
};

// ============================================================================
// METHODS - Plan Loading
// ============================================================================

const loadDraftPlan = async (articleId: string | undefined): Promise<void> => {
  if (!articleId) return;
  
  try {
    const plan = await apiClients.plans.getActivePlan(articleId);

    if (plan && plan.id) {
      openPlanTab(plan.id);
    }
  } catch (err: any) {
    if (err.response && err.response.status === 204) {
      return;
    }
    console.error('Error loading draft plan:', err);
  }
};

const loadLatestAIVersion = async (articleId: string | undefined): Promise<void> => {
  if (!articleId) return;
  
  // Use composable to find pending AI version by status
  const versionState = useVersionsState(computed(() => articleId));
  
  // Wait for versions to be loaded if not already
  if (!versionState.loaded.value) {
    // Versions will be loaded by VersionsPanel watch handler
    // Check again after a tick
    nextTick(() => {
      if (versionState.pendingAiVersion.value) {
        openVersionTab(versionState.pendingAiVersion.value);
      }
    });
    return;
  }

  // Use cached data - pendingAiVersion is found by status filter
  if (versionState.pendingAiVersion.value) {
    openVersionTab(versionState.pendingAiVersion.value);
  }
};

const handlePlanConversationCreated = async (conversationId: string): Promise<void> => {
  // Switch to the assistant tab
  setActiveRightTab('assistant');
  
  // Load the new conversation in the chat panel
  await nextTick();
  if (chatPanel.value) {
    await chatPanel.value.loadConversation(conversationId);
  }
};

// ============================================================================
// METHODS - Fragment Modal
// ============================================================================

const handleOpenFragment = async (fragmentId: string): Promise<void> => {
  if (!fragmentId) return;

  try {
    const fragment = await apiClients.fragments.get(fragmentId);
    selectedFragment.value = fragment;
  } catch (err: any) {
    console.error('Error loading fragment:', err);
    showToast('error', 'Failed to load fragment');
  }
};

const handleOpenVersion = async (versionId: string): Promise<void> => {
  if (!versionId) return;

  try {
    const version = await apiClients.articles.getVersion(articles.selectedId!, versionId);
    openVersionTab(version);
  } catch (err: any) {
    console.error('Error loading version:', err);
    showToast('error', 'Failed to load version');
  }
};

const closeFragmentModal = (): void => {
  selectedFragment.value = null;
};

const handleFragmentUpdated = (updatedFragment: FragmentDto): void => {
  // Update the selected fragment to show the new data
  selectedFragment.value = updatedFragment;
};

// ============================================================================
// METHODS - UI Utility
// ============================================================================

const setActiveRightTab = (tab: 'assistant' | 'versions'): void => {
  ui.activeRightTab = tab;
};

const setViewMode = (mode: 'tree' | 'list' | 'mywork'): void => {
  viewMode.value = mode;
  localStorage.setItem('articlesViewMode', mode);
};

const toggleViewMode = (): void => {
  const newMode = viewMode.value === 'tree' ? 'list' : 'tree';
  setViewMode(newMode);
};

// ============================================================================
// LIFECYCLE HOOKS
// ============================================================================

// Handle beforeunload event for unsaved changes
const handleBeforeUnload = (event: BeforeUnloadEvent) => {
  if (hasUnsavedChanges.value) {
    event.preventDefault();
    event.returnValue = 'You have unsaved changes. Are you sure you want to leave?';
    return event.returnValue;
  }
};

// Register cleanup hook at top level (before any async operations)
onBeforeUnmount(() => {
  window.removeEventListener('beforeunload', handleBeforeUnload);
  
  if (signalr.connection) {
    signalr.connection.stop()
      .then(() => console.log('Disconnected from ArticleHub'))
      .catch((err: any) => console.error('Error disconnecting from SignalR:', err));
  }
});

onMounted(async () => {
  // Restore saved view mode
  const savedViewMode = localStorage.getItem('articlesViewMode');
  if (savedViewMode && (savedViewMode === 'tree' || savedViewMode === 'list' || savedViewMode === 'mywork')) {
    viewMode.value = savedViewMode;
  }

  // Initialize SignalR composable
  signalRMethods = useArticleSignalR({
    insertArticleIntoTree,
    updateArticleInTree,
    removeArticleFromTree,
    moveArticleInTree,
    openPlanTab,
    openVersionTab,
    loadVersions: async () => {
      if (versionsPanel.value) {
        await versionsPanel.value.loadVersions();
      }
    },
    selectedArticleId: computed(() => articles.selectedId),
    articlesIndex: articles.index,
    clearSelectedArticle: () => {
      articles.selectedId = null;
      articles.selected = null;
      editor.title = '';
      editor.content = '';
    }
  });

  // Initialize and start SignalR connection
  try {
    await signalRMethods.initializeConnection();
    signalr.connection = signalRMethods.connection.value;
    console.log('Connected to ArticleHub');
  } catch (err: any) {
    console.error('SignalR connection error:', err);
  }

  // Load initial data
  await Promise.all([
    loadArticleTypes(),
    loadArticles()
  ]);

  // Sort articles
  treeOps.sortArticlesRecursive(articles.list);

  // Select article from URL if present
  const articleIdFromUrl = route.query.id as string | undefined;
  if (articleIdFromUrl) {
    const article = articles.index.get(articleIdFromUrl);
    if (article) {
      await selectArticle(article, true);
    }
  }

  // Register beforeunload event listener
  window.addEventListener('beforeunload', handleBeforeUnload);
});

// Watch for route changes (browser back/forward)
watch(() => route.query.id, async (newId) => {
  if (newId && typeof newId === 'string') {
    const article = articles.index.get(newId);
    if (article && article.id !== articles.selectedId) {
      await selectArticle(article, true);
    }
  } else if (!newId && articles.selectedId) {
    // Clear selection if no ID in URL
    articles.selectedId = null;
    articles.selected = null;
    editor.title = '';
    editor.content = '';
  }
});
</script>
