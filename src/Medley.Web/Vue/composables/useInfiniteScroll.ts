import { ref, onBeforeUnmount, nextTick, type Ref } from 'vue';

export interface PaginationState {
  page: number;
  pageSize: number;
  hasMore: boolean;
  loading: boolean;
  loadingMore: boolean;
}

export interface UseInfiniteScrollOptions {
  loadMoreItems: () => Promise<any[]>;
  pageSize?: number;
  scrollThreshold?: number;
}

/** Implements infinite scroll with pagination state and automatic loading */
export function useInfiniteScroll(options: UseInfiniteScrollOptions) {
  const pagination = ref<PaginationState>({
    page: 0,
    pageSize: options.pageSize || 50,
    hasMore: true,
    loading: false,
    loadingMore: false
  });

  let scrollContainer: HTMLElement | null = null;
  let scrollHandler: ((event: Event) => void) | null = null;
  let scrollThreshold = options.scrollThreshold || 100;

  const handleScroll = () => {
    if (!scrollContainer || !pagination.value.hasMore || pagination.value.loadingMore) {
      return;
    }

    const { scrollTop, clientHeight, scrollHeight } = scrollContainer;
    const distanceFromBottom = scrollHeight - (scrollTop + clientHeight);

    if (distanceFromBottom < scrollThreshold) {
      loadMore();
    }
  };

  const setupInfiniteScroll = (containerSelector: string, threshold?: number) => {
    if (threshold !== undefined) {
      scrollThreshold = threshold;
    }

    nextTick(() => {
      scrollContainer = document.querySelector(containerSelector);

      if (!scrollContainer) {
        console.warn(`Infinite scroll: Container not found: ${containerSelector}`);
        return;
      }

      scrollHandler = handleScroll;
      scrollContainer.addEventListener('scroll', scrollHandler, { passive: true });
    });
  };

  const loadMore = async () => {
    if (pagination.value.loadingMore || !pagination.value.hasMore) {
      return;
    }

    pagination.value.loadingMore = true;
    pagination.value.page++;

    try {
      const items = await options.loadMoreItems();

      if (!items || items.length < pagination.value.pageSize) {
        pagination.value.hasMore = false;
      }
    } catch (err) {
      console.error('Error loading more items:', err);
      pagination.value.page--;
    } finally {
      pagination.value.loadingMore = false;
    }
  };

  const resetPagination = (resetHasMore: boolean = true) => {
    pagination.value.page = 0;
    pagination.value.loading = false;
    pagination.value.loadingMore = false;
    if (resetHasMore) {
      pagination.value.hasMore = true;
    }
  };

  const updateHasMore = (items: any[] | null | undefined) => {
    if (!items || items.length < pagination.value.pageSize) {
      pagination.value.hasMore = false;
    } else {
      pagination.value.hasMore = true;
    }
  };

  const cleanupInfiniteScroll = () => {
    if (scrollContainer && scrollHandler) {
      scrollContainer.removeEventListener('scroll', scrollHandler);
      scrollContainer = null;
      scrollHandler = null;
    }
  };

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
