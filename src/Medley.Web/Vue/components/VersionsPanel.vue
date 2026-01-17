<template>
  <div class="d-flex flex-column h-100">
    <div class="sidebar-content flex-grow-1">
      <div v-if="!articleId" class="empty-state" v-cloak>
        <div class="empty-state-icon">
          <i class="bi bi-clock-history"></i>
        </div>
        <div class="empty-state-title">No Article Selected</div>
        <div class="empty-state-text">Select an article to view its version history</div>
      </div>
      <div v-else-if="loading" class="loading-spinner">
        <div class="spinner-border spinner-border-sm" role="status">
          <span class="visually-hidden">Loading...</span>
        </div>
      </div>
      <div v-else-if="error" class="alert alert-danger">
        {{ error }}
      </div>
      <div v-else-if="userVersions.length === 0" class="empty-state" v-cloak>
        <div class="empty-state-icon">
          <i class="bi bi-clock-history"></i>
        </div>
        <div class="empty-state-title">No versions yet</div>
        <div class="empty-state-text">Versions are created when you save changes to the article</div>
      </div>
      <div v-else class="versions-list">
        <div 
          v-for="version in userVersions" 
          :key="version.id" 
          class="version-item"
          :class="{ 'active': selectedVersionId === version.id }"
          @click="selectVersion(version)">
          <div class="version-header">
            <span class="badge bg-primary">v{{ version.versionNumber }}</span>
            <span class="version-date">{{ formatDate(version.createdAt) }}</span>
            <span v-if="version.createdByName" class="version-author">
              <i class="bi bi-person-fill"></i>
              {{ version.createdByName }}
            </span>
            <span v-else-if="version.createdByEmail" class="version-author">
              <i class="bi bi-person-fill"></i>
              {{ version.createdByEmail }}
            </span>
          </div>
          <div v-if="version.changeMessage" class="version-message">
            {{ version.changeMessage }}
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import { api } from '@/utils/api';
import { formatRelativeTime } from '@/utils/helpers';
import { useArticleVersions } from '@/composables/useArticleVersions';
import type { ArticleVersionDto } from '@/types/generated/api-client';

// Props
interface Props {
  articleId: string | null;
  selectedVersionId?: string | null;
}

const props = withDefaults(defineProps<Props>(), {
  articleId: null,
  selectedVersionId: null
});

// Emits
interface Emits {
  (e: 'select-version', version: ArticleVersionDto): void;
}

const emit = defineEmits<Emits>();

// Composable
const articleIdRef = computed(() => props.articleId);
const versionState = useArticleVersions(articleIdRef);

// Computed
const userVersions = computed(() => versionState.userVersions.value);
const loading = computed(() => versionState.loading.value);
const error = computed(() => versionState.error.value);

// Watch
watch(() => props.articleId, async (newArticleId) => {
  if (newArticleId) {
    await loadVersions();
  } else {
    versionState.setVersions([]);
  }
}, { immediate: true });

// Methods
async function loadVersions(): Promise<void> {
  if (!props.articleId) return;
  
  versionState.setLoading(true);
  versionState.setError(null);
  
  try {
    const versions = await api.get<ArticleVersionDto[]>(`/api/articles/${props.articleId}/versions`);
    versionState.setVersions(versions);
  } catch (err: any) {
    versionState.setError('Failed to load version history: ' + err.message);
    console.error('Error loading versions:', err);
  } finally {
    versionState.setLoading(false);
  }
}

function selectVersion(version: ArticleVersionDto): void {
  emit('select-version', version);
  // Collapse right sidebar on mobile after selection
  (window as any).MedleySidebar?.collapseRightSidebar();
}

function formatDate(dateString: Date | undefined): string {
  if (!dateString) return '';
  return formatRelativeTime(dateString, { short: false, includeTime: true });
}
</script>
