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
import { api } from '@/utils/api.js';
import { formatRelativeTime } from '@/utils/helpers.js';

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
  data() {
    return {
      versions: [],
      loading: false,
      error: null
    };
  },
  computed: {
    userVersions() {
      // Filter to show only User versions in the sidebar
      return this.versions.filter(v => v.versionType === 'User');
    }
  },
  watch: {
    articleId: {
      immediate: true,
      handler(newArticleId) {
        if (newArticleId) {
          this.loadVersions();
        } else {
          this.versions = [];
        }
      }
    }
  },
  methods: {
    async loadVersions() {
      if (!this.articleId) return;
      
      this.loading = true;
      this.error = null;
      
      try {
        // Using imported api
        this.versions = await api.get(`/api/articles/${this.articleId}/versions`);
      } catch (err) {
        this.error = 'Failed to load version history: ' + err.message;
        console.error('Error loading versions:', err);
      } finally {
        this.loading = false;
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
