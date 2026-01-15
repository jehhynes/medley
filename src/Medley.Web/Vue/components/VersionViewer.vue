<template>
  <div class="diff-viewer">
    <div v-if="loading" class="loading-spinner">
      <div class="spinner-border" role="status">
        <span class="visually-hidden">Loading version...</span>
      </div>
    </div>

    <div v-else-if="error" class="alert alert-danger m-3">
      {{ error }}
    </div>

    <template v-else-if="version">
      <!-- Version Header -->
      <div class="diff-viewer-header">
        <div class="diff-viewer-title">
          <!-- Accept/Reject buttons for AI versions -->
          <template v-if="isAIVersion">
            <button 
              type="button"
              class="btn btn-sm btn-success me-2"
              @click="acceptVersion"
              :disabled="isProcessing"
              title="Accept AI Version">
              <span v-if="isProcessing" class="spinner-border spinner-border-sm me-1" role="status"></span>
              <i v-else class="bi bi-check-lg me-1"></i>
              Accept
            </button>
            <button 
              type="button"
              class="btn btn-sm btn-outline-secondary me-2"
              @click="rejectVersion"
              :disabled="isProcessing"
              title="Reject AI Version">
              <i class="bi bi-x-lg me-1"></i>
              Reject
            </button>
          </template>
        </div>
        
        <!-- Version Dropdown -->
        <div class="d-flex align-items-center">
          <select 
            class="form-select form-select-sm"
            :value="version.id"
            @change="onVersionChange"
            style="min-width: 350px;">
            <option 
              v-for="v in allVersions" 
              :key="v.id" 
              :value="v.id">
              v{{ v.versionNumber }} - {{ formatDate(v.createdAt) }}{{ v.createdByName ? ' - ' + v.createdByName : '' }}
            </option>
          </select>
        </div>
      </div>

      <!-- Diff Content -->
      <div class="diff-viewer-content flex-grow-1">
        <!-- Version Change Message -->
        <div v-if="version.changeMessage" class="alert alert-info m-3">
          <i class="bi bi-info-circle me-2"></i>
          <strong>Changes:</strong> {{ version.changeMessage }}
        </div>

        <div v-if="loadingDiff" class="loading-spinner">
          <div class="spinner-border" role="status">
            <span class="visually-hidden">Loading diff...</span>
          </div>
        </div>
        <div v-else-if="diffError" class="alert alert-danger m-3">
          {{ diffError }}
        </div>
        <div v-else-if="diffHtml" v-html="diffHtml" class="diff-content markdown-container"></div>
      </div>
    </template>
  </div>
</template>

<script>
import { api } from '@/utils/api.js';
import { htmlDiff } from '@/utils/htmlDiff.js';

export default {
  name: 'VersionViewer',
  props: {
    articleId: {
      type: String,
      required: true
    },
    versionId: {
      type: String,
      required: true
    }
  },
  emits: ['version-changed', 'version-accepted', 'version-rejected'],
  data() {
    return {
      version: null,
      allVersions: [],
      loading: true,
      error: null,
      loadingDiff: false,
      diffError: null,
      diffHtml: null,
      isProcessing: false
    };
  },
  computed: {
    isAIVersion() {
      // Check if version type is AI
      return this.version && this.version.versionType === 'AI';
    }
  },
  watch: {
    versionId: {
      immediate: true,
      handler() {
        this.loadVersion();
      }
    }
  },
  methods: {
    async loadVersion() {
      this.loading = true;
      this.error = null;

      try {
        // Load all versions for dropdown (only once) - includes both User and AI versions
        if (this.allVersions.length === 0) {
          this.allVersions = await api.get(`/api/articles/${this.articleId}/versions`);
        }

        // Load specific version
        this.version = await api.get(`/api/articles/${this.articleId}/versions/${this.versionId}`);
        
        // Load the diff
        await this.loadVersionDiff();
      } catch (err) {
        console.error('Error loading version:', err);
        this.error = err.message || 'Failed to load version';
      } finally {
        this.loading = false;
      }
    },

    async loadVersionDiff() {
      this.loadingDiff = true;
      this.diffError = null;
      this.diffHtml = null;

      try {
        const response = await api.get(
          `/api/articles/${this.articleId}/versions/${this.versionId}/diff`
        );

        const beforeHtml = this.markdownToHtml(response.beforeContent || '');
        const afterHtml = this.markdownToHtml(response.afterContent || '');

        this.diffHtml = htmlDiff(beforeHtml, afterHtml);
      } catch (err) {
        this.diffError = 'Failed to load diff: ' + err.message;
        console.error('Error loading diff:', err);
      } finally {
        this.loadingDiff = false;
      }
    },

    async onVersionChange(event) {
      const versionId = event.target.value;
      if (versionId && versionId !== this.versionId) {
        this.$emit('version-changed', versionId);
      }
    },

    markdownToHtml(markdown) {
      if (!markdown) return '';
      
      // Use marked library if available
      if (window.marked) {
        try {
          return window.marked.parse(markdown, { 
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
    },

    formatDate(dateString) {
      if (!dateString) return '';
      const date = new Date(dateString);
      return date.toLocaleDateString() + ' ' + date.toLocaleTimeString();
    },

    async acceptVersion() {
      if (!this.version || this.isProcessing) return;

      bootbox.confirm({
        title: 'Accept AI Version',
        message: `Accept version ${this.version.versionNumber}? This will create a new user version based on this AI version.`,
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
        callback: async (result) => {
          if (result) {
            this.isProcessing = true;
            try {
              const response = await api.post(
                `/api/articles/${this.articleId}/versions/${this.version.id}/accept`
              );

              if (response) {
                // Emit event so parent can handle the refresh
                this.$emit('version-accepted', response);
              }
            } catch (err) {
              console.error('Error accepting version:', err);
              bootbox.alert({
                title: 'Error',
                message: 'Failed to accept version: ' + err.message,
                className: 'bootbox-error'
              });
            } finally {
              this.isProcessing = false;
            }
          }
        }
      });
    },

    async rejectVersion() {
      if (!this.version || this.isProcessing) return;

      bootbox.confirm({
        title: 'Reject AI Version',
        message: `Reject version ${this.version.versionNumber}? This version will be marked as inactive and won't auto-load anymore.`,
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
        callback: async (result) => {
          if (result) {
            this.isProcessing = true;
            try {
              await api.post(
                `/api/articles/${this.articleId}/versions/${this.version.id}/reject`
              );

              // Emit event so parent can handle the refresh
              this.$emit('version-rejected', this.version.id);
            } catch (err) {
              console.error('Error rejecting version:', err);
              bootbox.alert({
                title: 'Error',
                message: 'Failed to reject version: ' + err.message,
                className: 'bootbox-error'
              });
            } finally {
              this.isProcessing = false;
            }
          }
        }
      });
    }
  }
};
</script>
