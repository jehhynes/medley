import { ref, onBeforeUnmount, nextTick, type Ref } from 'vue';

/**
 * Pagination state interface
 */
export interface PaginationState {
  page: number;
  pageSize: number;
  hasMore: boolean;
  loading: boolean;
  loadingMore: boolean;
}

/**
 * Options for useInfiniteScroll composable
 */
export interface UseInfiniteScrollOptions {
  /**
   * Function to load more items (next page)
   * Should return the array of items loaded
   */
  loadMoreItems: () => Promise<any[]>;

  /**
   * Initial page size (default: 50)
   */
  pageSize?: number;

  /**
   * Distance from bottom in pixels to trigger load (default: 100)
   */
  scrollThreshold?: number;
}

/**
 * Return type for useInfiniteScroll composable
 */
interface UseInfiniteScrollReturn {
  pagination: Ref<PaginationState>;
  setupInfiniteScroll: (containerSelector: string, threshold?: number) => void;
  loadMore: () => Promise<void>;
  resetPagination: (resetHasMore?: boolean) => void;
  updateHasMore: (items: any[] | null | undefined) => void;
  cleanupInfiniteScroll: () => void;
}

/**
 * Composable for implementing infinite scroll functionality.
 * Handles pagination state, scroll detection, and automatic loading of more items.
 * 
 * @param options - Configuration options for infinite scroll
 * @returns Pagination state and control methods
 * 
 * @example
 * ```typescript
 * const { pagination, setupInfiniteScroll, loadMore, resetPagination } = useInfiniteScroll({
 *   loadMoreItems: async () => {
 *     const skip = pagination.value.page * pagination.value.pageSize;
 *     const items = await api.get(`/api/items?skip=${skip}&take=${pagination.value.pageSize}`);
 *     return items;
 *   },
 *   pageSize: 50
 * });
 * 
 * onMounted(() => {
 *   setupInfiniteScroll('.sidebar-content');
 * });
 * ```
 */
export function useInfiniteScroll(options: UseInfiniteScrollOptions): UseInfiniteScrollReturn {
  const pagination = ref<PaginationState>({
    page: 0,
    pageSize: options.pageSize || 50,
    hasMore: true,
    loading: false,
    loadingMore: false
  });

  // Internal state
  let scrollContainer: HTMLElement | null = null;
  let scrollHandler: ((event: Event) => void) | null = null;
  let scrollThreshold = options.scrollThreshold || 100;

  /**
   * Handle scroll events and trigger load more when near bottom
   */
  const handleScroll = (): void => {
    if (!scrollContainer || !pagination.value.hasMore || pagination.value.loadingMore) {
      return;
    }

    const { scrollTop, clientHeight, scrollHeight } = scrollContainer;
    const distanceFromBottom = scrollHeight - (scrollTop + clientHeight);

    // Trigger load when within threshold of bottom
    if (distanceFromBottom < scrollThreshold) {
      loadMore();
    }
  };

  /**
   * Setup infinite scroll on a container element
   * 
   * @param containerSelector - CSS selector for scroll container
   * @param threshold - Distance from bottom in pixels to trigger load (optional)
   */
  const setupInfiniteScroll = (containerSelector: string, threshold?: number): void => {
    if (threshold !== undefined) {
      scrollThreshold = threshold;
    }

    // Wait for next tick to ensure DOM is ready
    nextTick(() => {
      scrollContainer = document.querySelector(containerSelector);

      if (!scrollContainer) {
        console.warn(`Infinite scroll: Container not found: ${containerSelector}`);
        return;
      }

      // Create bound handler for cleanup
      scrollHandler = handleScroll;

      // Add scroll listener
      scrollContainer.addEventListener('scroll', scrollHandler, { passive: true });
    });
  };

  /**
   * Load more items (next page)
   */
  const loadMore = async (): Promise<void> => {
    if (pagination.value.loadingMore || !pagination.value.hasMore) {
      return;
    }

    pagination.value.loadingMore = true;
    pagination.value.page++;

    try {
      const items = await options.loadMoreItems();

      // If we got fewer items than page size, we've reached the end
      if (!items || items.length < pagination.value.pageSize) {
        pagination.value.hasMore = false;
      }
    } catch (err) {
      console.error('Error loading more items:', err);
      // Revert page increment on error
      pagination.value.page--;
    } finally {
      pagination.value.loadingMore = false;
    }
  };

  /**
   * Reset pagination state (call this when filters/search changes)
   * 
   * @param resetHasMore - Whether to reset hasMore flag (default: true)
   */
  const resetPagination = (resetHasMore: boolean = true): void => {
    pagination.value.page = 0;
    pagination.value.loading = false;
    pagination.value.loadingMore = false;
    if (resetHasMore) {
      pagination.value.hasMore = true;
    }
  };

  /**
   * Update hasMore flag based on returned items count
   * 
   * @param items - Array of items returned from API
   */
  const updateHasMore = (items: any[] | null | undefined): void => {
    if (!items || items.length < pagination.value.pageSize) {
      pagination.value.hasMore = false;
    } else {
      pagination.value.hasMore = true;
    }
  };

  /**
   * Cleanup scroll listeners
   */
  const cleanupInfiniteScroll = (): void => {
    if (scrollContainer && scrollHandler) {
      scrollContainer.removeEventListener('scroll', scrollHandler);
      scrollContainer = null;
      scrollHandler = null;
    }
  };

  // Automatically cleanup scroll listeners on unmount
  onBeforeUnmount(() => {
    cleanupInfiniteScroll();
  });

  return {
    pagination,
    setupInfiniteScroll,
    loadMore,
    resetPagination,
    updateHasMore,
    cleanupInfiniteScroll
  };
}
