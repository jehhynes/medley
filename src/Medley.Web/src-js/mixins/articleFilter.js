// Article Filter Mixin - Handles article filtering modal and state
export default {
    data() {
        return {
            // Filter modal state
            filterModal: {
                visible: false
            },
            
            // Filter values
            filters: {
                query: '',
                statuses: [],
                articleTypeIds: []
            }
        };
    },
    
    computed: {
        /**
         * Check if any filters are currently active
         * @returns {boolean}
         */
        hasActiveFilters() {
            return this.filters.query.trim() !== '' ||
                   this.filters.statuses.length > 0 ||
                   this.filters.articleTypeIds.length > 0;
        },
        
        /**
         * Count of active filters for badge display
         * @returns {number}
         */
        activeFilterCount() {
            let count = 0;
            if (this.filters.query.trim() !== '') count++;
            if (this.filters.statuses.length > 0) count++;
            if (this.filters.articleTypeIds.length > 0) count++;
            return count;
        }
    },
    
    methods: {
        /**
         * Show the filter modal
         */
        showFilterModal() {
            this.filterModal.visible = true;
            // Focus on search input after modal is shown
            this.$nextTick(() => {
                const searchInput = this.$refs.filterSearchInput;
                if (searchInput) {
                    searchInput.focus();
                }
            });
        },
        
        /**
         * Close the filter modal
         */
        closeFilterModal() {
            this.filterModal.visible = false;
        },
        
        /**
         * Apply the current filters and reload articles
         */
        async applyFilters() {
            this.closeFilterModal();
            await this.loadArticles();
        },
        
        /**
         * Clear all filters and reload articles
         */
        async clearFilters() {
            this.filters.query = '';
            this.filters.statuses = [];
            this.filters.articleTypeIds = [];
            this.closeFilterModal();
            await this.loadArticles();
        },
        
        /**
         * Toggle a status in the filter
         * @param {number} status - ArticleStatus enum value
         */
        toggleStatusFilter(status) {
            const index = this.filters.statuses.indexOf(status);
            if (index > -1) {
                this.filters.statuses.splice(index, 1);
            } else {
                this.filters.statuses.push(status);
            }
        },
        
        /**
         * Check if a status is selected
         * @param {number} status - ArticleStatus enum value
         * @returns {boolean}
         */
        isStatusSelected(status) {
            return this.filters.statuses.includes(status);
        },
        
        /**
         * Toggle an article type in the filter
         * @param {string} typeId - Article type ID
         */
        toggleArticleTypeFilter(typeId) {
            const index = this.filters.articleTypeIds.indexOf(typeId);
            if (index > -1) {
                this.filters.articleTypeIds.splice(index, 1);
            } else {
                this.filters.articleTypeIds.push(typeId);
            }
        },
        
        /**
         * Check if an article type is selected
         * @param {string} typeId - Article type ID
         * @returns {boolean}
         */
        isArticleTypeSelected(typeId) {
            return this.filters.articleTypeIds.includes(typeId);
        },
        
        /**
         * Build query string from current filter state
         * @returns {string}
         */
        buildFilterQueryString() {
            const params = new URLSearchParams();
            
            if (this.filters.query.trim() !== '') {
                params.append('query', this.filters.query.trim());
            }
            
            this.filters.statuses.forEach(status => {
                params.append('statuses', status.toString());
            });
            
            this.filters.articleTypeIds.forEach(typeId => {
                params.append('articleTypeIds', typeId);
            });
            
            const queryString = params.toString();
            return queryString ? `?${queryString}` : '';
        },
        
        /**
         * Get status label for display
         * @param {number} status - ArticleStatus enum value
         * @returns {string}
         */
        getStatusLabel(status) {
            const labels = {
                0: 'Draft',
                1: 'Review',
                2: 'Approved',
                3: 'Archived'
            };
            return labels[status] || 'Unknown';
        },
        
        /**
         * Get icon class for article type, handling bi- and fa- prefixes
         * @param {string} icon - Icon string from article type
         * @returns {string}
         */
        getArticleTypeIconClass(icon) {
            if (!icon) {
                return 'bi bi-file-text';
            }
            if (icon.startsWith('bi-')) {
                return `bi ${icon}`;
            }
            if (icon.startsWith('fa-')) {
                return `fas ${icon}`;
            }
            return 'bi bi-file-text';
        }
    }
};
