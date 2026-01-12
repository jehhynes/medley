// Article Version Mixin - Handles version history and diff viewing
export default {
    methods: {
        // === Version History ===
        async handleVersionSelect(version) {
            if (!version || !this.articles.selectedId) return;
            
            // Open version in a new tab instead of replacing content
            this.openVersionTab(version);
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

        clearVersionSelection() {
            // Switch back to editor tab
            if (this.switchContentTab) {
                this.switchContentTab('editor');
            }
            
            // Clear old version state (for backwards compatibility)
            this.version.selected = null;
            this.version.diffHtml = null;
            this.version.diffError = null;
        }
    }
};
