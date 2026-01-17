<template>
  <div class="diff-viewer">
    <div v-if="loading" class="loading-spinner">
      <div class="spinner-border" role="status">
        <span class="visually-hidden">Loading version...</span>
      </div>
    </div>

    <div v-else-if="error" class="alert alert-danger">
      {{ error }}
    </div>

    <template v-else-if="version">
      <!-- Version Header -->
      <div class="diff-viewer-header">
        <div class="diff-viewer-title">
          <!-- Accept/Reject buttons for AI versions -->
          <template v-if="isAIVersion && canReviewVersion">
            <button 
              type="button"
              class="tiptap-toolbar-btn tiptap-btn-with-text is-active"
              @click="acceptVersion"
              :disabled="isProcessing"
              title="Accept AI Version">
              <span v-if="isProcessing" class="spinner-border spinner-border-sm me-1" role="status"></span>
              <i v-else class="bi bi-check-lg me-1"></i>
              Accept
            </button>
            <button 
              type="button"
              class="tiptap-toolbar-btn tiptap-btn-with-text"
              @click="rejectVersion"
              :disabled="isProcessing"
              title="Reject AI Version">
              <i class="bi bi-x-lg me-1"></i>
              Reject
            </button>
            <div class="tiptap-toolbar-divider"></div>
          </template>
          
          <!-- Current Version Status (middle area) -->
          <div class="d-flex align-items-center me-auto">
            <span>
              <strong class="fs-6">{{ getBaseStatusLabel(version.status) }}</strong>
              <span v-if="getStatusDetails(version)" class="text-muted small ms-2">
                {{ getStatusDetails(version) }}
              </span>
            </span>
          </div>
        </div>
        
        <!-- Version Dropdown -->
        <div class="tiptap-dropdown active position-relative" ref="versionDropdown">
          <button 
            type="button"
            class="tiptap-toolbar-btn tiptap-btn-with-text"
            @click="toggleVersionDropdown"
            title="Select version">
            v{{ version.versionNumber }}
            <i class="bi bi-chevron-down ms-1"></i>
          </button>
          <div 
            v-if="versionDropdownOpen" 
            class="tiptap-dropdown-menu right-aligned"
            style="min-width: 400px;"
            @click.stop>
            <div 
              v-for="v in allVersions" 
              :key="v.id" 
              class="tiptap-dropdown-item"
              :class="{ 'is-active': v.id === version.id }"
              @click="selectVersion(v.id)">
              <div class="d-flex justify-content-between align-items-center gap-3 w-100">
                <strong>v{{ v.versionNumber }}</strong>
                <span class="text-muted small">{{ formatDate(v.createdAt) }}</span>
                <span class="text-muted small" v-if="v.createdByName">{{ v.createdByName }}</span>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Diff Content -->
      <div class="diff-viewer-content flex-grow-1">
        <!-- Version Change Message -->
        <div v-if="version.changeMessage" class="alert alert-info">
          {{ version.changeMessage }}
        </div>

        <div v-if="loadingDiff" class="loading-spinner">
          <div class="spinner-border" role="status">
            <span class="visually-hidden">Loading diff...</span>
          </div>
        </div>
        <div v-else-if="diffError" class="alert alert-danger">
          {{ diffError }}
        </div>
        <div v-else-if="diffHtml" v-html="diffHtml" class="diff-content markdown-container"></div>
      </div>
    </template>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted, onBeforeUnmount } from 'vue';
import { api } from '@/utils/api';
import { htmlDiff } from '@/utils/htmlDiff';
import { useArticleVersions } from '@/composables/useArticleVersions';
import type { ArticleVersionDto, ArticleVersionComparisonDto, VersionType } from '@/types/generated/api-client';

// Declare global types
declare const marked: {
  parse: (markdown: string, options?: any) => string;
};

declare const bootbox: {
  confirm: (options: {
    title: string;
    message: string;
    buttons: {
      confirm: { label: string; className: string };
      cancel: { label: string; className: string };
    };
    callback: (result: boolean) => void;
  }) => void;
  alert: (options: {
    title: string;
    message: string;
    className?: string;
  }) => void;
};

// Extended version interface with computed status
interface ArticleVersionWithStatus extends ArticleVersionDto {
  status?: string;
  createdByName?: string;
  createdByEmail?: string;
}

// Props
interface Props {
  articleId: string;
  versionId: string;
}

const props = defineProps<Props>();

// Emits
interface Emits {
  (e: 'version-changed', versionId: string): void;
  (e: 'version-accepted', response: any): void;
  (e: 'version-rejected', versionId: string): void;
}

const emit = defineEmits<Emits>();

// Composable
const articleIdRef = computed(() => props.articleId);
const versionState = useArticleVersions(articleIdRef);

// State
const version = ref<ArticleVersionWithStatus | null>(null);
const allVersions = ref<ArticleVersionDto[]>([]);
const loading = ref<boolean>(true);
const error = ref<string | null>(null);
const loadingDiff = ref<boolean>(false);
const diffError = ref<string | null>(null);
const diffHtml = ref<string | null>(null);
const isProcessing = ref<boolean>(false);
const versionDropdownOpen = ref<boolean>(false);
const versionDropdown = ref<HTMLElement | null>(null);

// Computed
const isAIVersion = computed<boolean>(() => {
  return version.value?.versionType === 'AI' as VersionType;
});

const canReviewVersion = computed<boolean>(() => {
  return version.value?.status === 'PendingAiVersion';
});

// Watch
watch(() => props.versionId, () => {
  loadVersion();
}, { immediate: true });

// Methods
async function loadVersion(): Promise<void> {
  loading.value = true;
  error.value = null;

  try {
    // Use cached versions from composable
    allVersions.value = versionState.versions.value;
    
    // Find specific version in cached data using helper
    const foundVersion = versionState.getVersionById(props.versionId);
    
    if (!foundVersion) {
      throw new Error('Version not found');
    }
    
    version.value = foundVersion as ArticleVersionWithStatus;
    
    // Load the diff
    await loadVersionDiff();
  } catch (err: any) {
    console.error('Error loading version:', err);
    error.value = err.message || 'Failed to load version';
  } finally {
    loading.value = false;
  }
}

async function loadVersionDiff(): Promise<void> {
  loadingDiff.value = true;
  diffError.value = null;
  diffHtml.value = null;

  try {
    const response = await api.get<ArticleVersionComparisonDto>(
      `/api/articles/${props.articleId}/versions/${props.versionId}/diff`
    );

    const beforeHtml = markdownToHtml(response.beforeContent || '');
    const afterHtml = markdownToHtml(response.afterContent || '');

    diffHtml.value = htmlDiff(beforeHtml, afterHtml);
  } catch (err: any) {
    diffError.value = 'Failed to load diff: ' + err.message;
    console.error('Error loading diff:', err);
  } finally {
    loadingDiff.value = false;
  }
}

function toggleVersionDropdown(): void {
  versionDropdownOpen.value = !versionDropdownOpen.value;
}

async function selectVersion(versionId: string): Promise<void> {
  if (versionId && versionId !== props.versionId) {
    versionDropdownOpen.value = false;
    emit('version-changed', versionId);
  }
}

function handleClickOutside(event: MouseEvent): void {
  if (versionDropdown.value && !versionDropdown.value.contains(event.target as Node)) {
    versionDropdownOpen.value = false;
  }
}

function markdownToHtml(markdown: string): string {
  if (!markdown) return '';
  
  // Use marked library if available
  if (typeof marked !== 'undefined') {
    try {
      return marked.parse(markdown, { 
        breaks: true, 
        gfm: true,
        headerIds: false,
        mangle: false
      });
    } catch (e) {
      console.error('Failed to parse markdown:', e);
      return markdown;
    }
  }
  
  // Fallback: return markdown as-is wrapped in pre
  return `<pre>${markdown}</pre>`;
}

function formatDate(dateString: Date | undefined): string {
  if (!dateString) return '';
  const date = new Date(dateString);
  return date.toLocaleDateString() + ' ' + date.toLocaleTimeString();
}

function getBaseStatusLabel(status: string | undefined): string {
  if (!status) return '';
  
  const labels: Record<string, string> = {
    'CurrentVersion': 'Current Version',
    'OldVersion': 'Prior Version',
    'PendingAiVersion': 'AI Update',
    'AcceptedAiVersion': 'AI Update',
    'RejectedAiVersion': 'AI Update',
    'OldAiVersion': 'AI Update'
  };
  return labels[status] || status;
}

function getStatusDetails(version: ArticleVersionWithStatus | null): string {
  if (!version) return '';
  
  const status = version.status;
  const userName = version.createdByName;
  const date = version.createdAt ? formatDate(version.createdAt) : '';
  
  switch (status) {
    case 'AcceptedAiVersion':
      return userName && date ? `Accepted by ${userName} on ${date}` : 'Accepted';
    case 'RejectedAiVersion':
      return userName && date ? `Rejected by ${userName} on ${date}` : 'Rejected';
    case 'CurrentVersion':
    case 'OldVersion':
      return userName && date ? `by ${userName} on ${date}` : '';
    case 'PendingAiVersion':
      return 'Pending';
    case 'OldAiVersion':
      return 'Archived';
    default:
      return '';
  }
}

async function acceptVersion(): Promise<void> {
  if (!version.value || isProcessing.value) return;

  bootbox.confirm({
    title: 'Accept AI Version',
    message: `Accept version ${version.value.versionNumber}? This will create a new user version based on this AI version.`,
    buttons: {
      confirm: {
        label: 'Accept',
        className: 'btn-success'
      },
      cancel: {
        label: 'Cancel',
        className: 'btn-secondary'
      }
    },
    callback: async (result: boolean) => {
      if (result) {
        isProcessing.value = true;
        try {
          const response = await api.post<void, any>(
            `/api/articles/${props.articleId}/versions/${version.value!.id}/accept`,
            undefined as any
          );

          if (response) {
            // Emit event so parent can handle the refresh
            emit('version-accepted', response);
          }
        } catch (err: any) {
          console.error('Error accepting version:', err);
          bootbox.alert({
            title: 'Error',
            message: 'Failed to accept version: ' + err.message,
            className: 'bootbox-error'
          });
        } finally {
          isProcessing.value = false;
        }
      }
    }
  });
}

async function rejectVersion(): Promise<void> {
  if (!version.value || isProcessing.value) return;

  bootbox.confirm({
    title: 'Reject AI Version',
    message: `Reject version ${version.value.versionNumber}? This version will be marked as rejected and won't auto-load anymore.`,
    buttons: {
      confirm: {
        label: 'Reject',
        className: 'btn-danger'
      },
      cancel: {
        label: 'Cancel',
        className: 'btn-secondary'
      }
    },
    callback: async (result: boolean) => {
      if (result) {
        isProcessing.value = true;
        try {
          await api.post<void, void>(
            `/api/articles/${props.articleId}/versions/${version.value!.id}/reject`,
            undefined as any
          );

          // Emit event so parent can handle the refresh
          emit('version-rejected', version.value!.id!);
        } catch (err: any) {
          console.error('Error rejecting version:', err);
          bootbox.alert({
            title: 'Error',
            message: 'Failed to reject version: ' + err.message,
            className: 'bootbox-error'
          });
        } finally {
          isProcessing.value = false;
        }
      }
    }
  });
}

// Lifecycle
onMounted(() => {
  document.addEventListener('click', handleClickOutside);
});

onBeforeUnmount(() => {
  document.removeEventListener('click', handleClickOutside);
});
</script>
