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
            <span v-if="version.createdBy?.fullName" class="version-author">
              <i class="bi bi-person-fill"></i>
              {{ version.createdBy.fullName }}
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
import { computed, inject } from 'vue';
import { formatRelativeTime } from '@/utils/helpers';
import type { ArticleVersionsStore } from '../stores/useArticleVersionsStore';
import type { ArticleVersionDto } from '@/types/api-client';

interface Props {
  articleId: string | null;
  selectedVersionId?: string | null;
}

const props = withDefaults(defineProps<Props>(), {
  articleId: null,
  selectedVersionId: null
});

interface Emits {
  (e: 'select-version', version: ArticleVersionDto): void;
}

const emit = defineEmits<Emits>();

// Inject versions store
const versionsStore = inject<ArticleVersionsStore>('versionsStore');

if (!versionsStore) {
  throw new Error('VersionsPanel must be used within Articles page');
}

// Use store data
const userVersions = computed(() => versionsStore.userVersions.value);
const loading = computed(() => versionsStore.loading.value);
const error = computed(() => versionsStore.error.value);

function selectVersion(version: ArticleVersionDto): void {
  emit('select-version', version);
  (window as any).MedleySidebar?.collapseRightSidebar();
}

function openLatestVersion(): void {
  // Use the already-loaded versions from the store
  const allVersions = versionsStore.versions.value;
  
  if (allVersions.length === 0) {
    return; // No versions available
  }

  // Find latest AI version (status = PendingAiVersion and type = AI)
  const latestAiVersion = allVersions.find(v => 
    v.status === 'PendingAiVersion' &&
    v.versionType === 'AI'
  );

  if (latestAiVersion) {
    selectVersion(latestAiVersion);
    return;
  }

  // If no AI version, get the latest user version (most recent one)
  const latestVersion = allVersions[0]; // Versions are ordered by version number descending
  if (latestVersion) {
    selectVersion(latestVersion);
  }
}

function formatDate(dateString: Date | undefined): string {
  if (!dateString) return '';
  return formatRelativeTime(dateString, { short: false, includeTime: true });
}

// Expose methods to parent component
defineExpose({
  openLatestVersion
});
</script>
