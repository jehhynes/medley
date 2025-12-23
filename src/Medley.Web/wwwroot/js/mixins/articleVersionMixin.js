// Article Version Mixin - Handles version history and diff viewing
(function() {
    const { api } = window.MedleyApi;
    
    window.articleVersionMixin = {
        methods: {
            // === Version History ===
            async handleVersionSelect(version) {
                if (!version || !this.articles.selectedId) return;
                
                this.version.selected = version;
                this.version.loadingDiff = true;
                this.version.diffError = null;
                this.version.diffHtml = null;
                
                try {
                    const response = await api.get(
                        `/api/articles/${this.articles.selectedId}/versions/${version.id}/diff`
                    );
                    
                    // Convert markdown to HTML for both versions
                    const beforeHtml = this.markdownToHtml(response.beforeContent || '');
                    const afterHtml = this.markdownToHtml(response.afterContent || '');
                    
                    // Use htmlDiff to compare the HTML versions
                    this.version.diffHtml = window.HtmlDiff.htmlDiff(beforeHtml, afterHtml);
                } catch (err) {
                    this.version.diffError = 'Failed to load diff: ' + err.message;
                    console.error('Error loading diff:', err);
                } finally {
                    this.version.loadingDiff = false;
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

            clearVersionSelection() {
                this.version.selected = null;
                this.version.diffHtml = null;
                this.version.diffError = null;
            }
        }
    };
})();

