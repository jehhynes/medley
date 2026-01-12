// Infinite Scroll Mixin - Reusable pagination and scroll detection for lists
/**
 * Mixin for implementing infinite scroll functionality
 * 
 * Usage:
 * 1. Add this mixin to your Vue component
 * 2. Implement loadInitialItems() and loadMoreItems() methods
 * 3. Call setupInfiniteScroll(containerSelector) in mounted hook
 * 4. Call resetPagination() when filters/search changes
 * 
 * Example:
 *   import infiniteScrollMixin from '@/mixins/infinite-scroll';
 *   
 *   export default {
 *     mixins: [infiniteScrollMixin],
 *     methods: {
 *       async loadInitialItems() {
 *         const items = await api.get(`/api/items?skip=0&take=${this.pagination.pageSize}`);
 *         this.items = items;
 *         return items;
 *       },
 *       async loadMoreItems() {
 *         const skip = this.pagination.page * this.pagination.pageSize;
 *         const items = await api.get(`/api/items?skip=${skip}&take=${this.pagination.pageSize}`);
 *         this.items.push(...items);
 *         return items;
 *       }
 *     },
 *     async mounted() {
 *       await this.loadInitialItems();
 *       this.setupInfiniteScroll('.sidebar-content');
 *     }
 *   }
 */
export default {
    data() {
        return {
            pagination: {
                page: 0,
                pageSize: 50,
                hasMore: true,
                loading: false,
                loadingMore: false
            },
            // Internal state
            _scrollContainer: null,
            _scrollHandler: null,
            _scrollThreshold: 100 // pixels from bottom to trigger load
        };
    },
    
    methods: {
        /**
         * Setup infinite scroll on a container element
         * @param {string} containerSelector - CSS selector for scroll container
         * @param {number} threshold - Distance from bottom in pixels to trigger load (default: 100)
         */
        setupInfiniteScroll(containerSelector, threshold = 100) {
            this._scrollThreshold = threshold;
            
            // Wait for next tick to ensure DOM is ready
            this.$nextTick(() => {
                this._scrollContainer = document.querySelector(containerSelector);
                
                if (!this._scrollContainer) {
                    console.warn(`Infinite scroll: Container not found: ${containerSelector}`);
                    return;
                }
                
                // Create bound handler for cleanup
                this._scrollHandler = this.handleScroll.bind(this);
                
                // Add scroll listener
                this._scrollContainer.addEventListener('scroll', this._scrollHandler, { passive: true });
            });
        },
        
        /**
         * Handle scroll events and trigger load more when near bottom
         */
        handleScroll() {
            if (!this._scrollContainer || !this.pagination.hasMore || this.pagination.loadingMore) {
                return;
            }
            
            const { scrollTop, clientHeight, scrollHeight } = this._scrollContainer;
            const distanceFromBottom = scrollHeight - (scrollTop + clientHeight);
            
            // Trigger load when within threshold of bottom
            if (distanceFromBottom < this._scrollThreshold) {
                this.loadMore();
            }
        },
        
        /**
         * Load more items (next page)
         * Calls the loadMoreItems() method which must be implemented by the component
         */
        async loadMore() {
            if (this.pagination.loadingMore || !this.pagination.hasMore) {
                return;
            }
            
            this.pagination.loadingMore = true;
            this.pagination.page++;
            
            try {
                // Call the component's loadMoreItems method
                if (typeof this.loadMoreItems !== 'function') {
                    console.error('infiniteScrollMixin: loadMoreItems() method must be implemented');
                    return;
                }
                
                const items = await this.loadMoreItems();
                
                // If we got fewer items than page size, we've reached the end
                if (!items || items.length < this.pagination.pageSize) {
                    this.pagination.hasMore = false;
                }
            } catch (err) {
                console.error('Error loading more items:', err);
                // Revert page increment on error
                this.pagination.page--;
            } finally {
                this.pagination.loadingMore = false;
            }
        },
        
        /**
         * Reset pagination state (call this when filters/search changes)
         * @param {boolean} resetHasMore - Whether to reset hasMore flag (default: true)
         */
        resetPagination(resetHasMore = true) {
            this.pagination.page = 0;
            this.pagination.loading = false;
            this.pagination.loadingMore = false;
            if (resetHasMore) {
                this.pagination.hasMore = true;
            }
        },
        
        /**
         * Update hasMore flag based on returned items count
         * @param {Array} items - Array of items returned from API
         */
        updateHasMore(items) {
            if (!items || items.length < this.pagination.pageSize) {
                this.pagination.hasMore = false;
            } else {
                this.pagination.hasMore = true;
            }
        },
        
        /**
         * Cleanup scroll listeners
         */
        cleanupInfiniteScroll() {
            if (this._scrollContainer && this._scrollHandler) {
                this._scrollContainer.removeEventListener('scroll', this._scrollHandler);
                this._scrollContainer = null;
                this._scrollHandler = null;
            }
        }
    },
    
    beforeUnmount() {
        // Automatically cleanup scroll listeners
        this.cleanupInfiniteScroll();
    }
};
