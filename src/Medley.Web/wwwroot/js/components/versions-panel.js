// Versions Panel Component - Version history sidebar for articles
const VersionsPanel = {
    name: 'VersionsPanel',
    template: `
  <div class="d-flex flex-column h-100">
    <div class="sidebar-content flex-grow-1">
      <div v-if="!articleId" class="empty-state">
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
      <div v-else-if="versions.length === 0" class="empty-state">
        <div class="empty-state-icon">
          <i class="bi bi-clock-history"></i>
        </div>
        <div class="empty-state-title">No versions yet</div>
        <div class="empty-state-text">Versions are created when you save changes to the article</div>
      </div>
      <div v-else class="versions-list">
        <div 
          v-for="version in versions" 
          :key="version.id" 
          class="version-item"
          :class="{ 'active': selectedVersionId === version.id }"
          @click="selectVersion(version)">
          <div class="version-header">
            <span class="badge bg-primary">v{{ version.versionNumber }}</span>
            <span class="version-date">{{ formatDate(version.createdAt) }}</span>
          </div>
          <div v-if="version.createdByName" class="version-author">
            <i class="bi bi-person-fill"></i>
            {{ version.createdByName }}
          </div>
          <div v-else-if="version.createdByEmail" class="version-author">
            <i class="bi bi-person-fill"></i>
            {{ version.createdByEmail }}
          </div>
        </div>
      </div>
    </div>
  </div>
    `,
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
    data() {
        return {
            versions: [],
            loading: false,
            error: null
        };
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
                const { api } = window.MedleyApi;
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
        },
        formatDate(dateString) {
            const date = new Date(dateString);
            const now = new Date();
            const diffMs = now - date;
            const diffMins = Math.floor(diffMs / 60000);
            const diffHours = Math.floor(diffMs / 3600000);
            const diffDays = Math.floor(diffMs / 86400000);
            
            if (diffMins < 1) return 'Just now';
            if (diffMins < 60) return `${diffMins} minute${diffMins > 1 ? 's' : ''} ago`;
            if (diffHours < 24) return `${diffHours} hour${diffHours > 1 ? 's' : ''} ago`;
            if (diffDays < 7) return `${diffDays} day${diffDays > 1 ? 's' : ''} ago`;
            
            return date.toLocaleDateString([], { 
                year: 'numeric', 
                month: 'short', 
                day: 'numeric',
                hour: '2-digit',
                minute: '2-digit'
            });
        }
    }
};

