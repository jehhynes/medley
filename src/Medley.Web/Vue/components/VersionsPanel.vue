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

<script>
import { computed } from 'vue';
import { api } from '@/utils/api.js';
import { formatRelativeTime } from '@/utils/helpers.js';
import { useArticleVersions } from '@/composables/useArticleVersions.js';

export default {
  name: 'VersionsPanel',
  props: {
    articleId: {
      type: String,
      default: null
    },
    selectedVersionId: {
      type: String,
      default: null
    }
  },
  emits: ['select-version'],
  setup(props) {
    const articleId = computed(() => props.articleId);
    const versionState = useArticleVersions(articleId);
    return { versionState };
  },
  computed: {
    userVersions() {
      return this.versionState.userVersions.value;
    },
    loading() {
      return this.versionState.loading.value;
    },
    error() {
      return this.versionState.error.value;
    }
  },
  watch: {
    articleId: {
      immediate: true,
      handler(newArticleId) {
        if (newArticleId) {
          this.loadVersions();
        } else {
          this.versionState.setVersions([]);
        }
      }
    }
  },
  methods: {
    async loadVersions() {
      if (!this.articleId) return;
      
      this.versionState.setLoading(true);
      this.versionState.setError(null);
      
      try {
        const versions = await api.get(`/api/articles/${this.articleId}/versions`);
        this.versionState.setVersions(versions);
      } catch (err) {
        this.versionState.setError('Failed to load version history: ' + err.message);
        console.error('Error loading versions:', err);
      } finally {
        this.versionState.setLoading(false);
      }
    },
    selectVersion(version) {
      this.$emit('select-version', version);
      // Collapse right sidebar on mobile after selection
      window.MedleySidebar?.collapseRightSidebar();
    },
    formatDate(dateString) {
      return formatRelativeTime(dateString, { short: false, includeTime: true });
    }
  }
};
</script>
