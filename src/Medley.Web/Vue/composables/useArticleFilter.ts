import { ref, computed, nextTick, type Ref, type ComputedRef } from 'vue';
import type { ArticleStatus } from '@/types/generated/api-client';

/**
 * Filter state interface
 */
export interface ArticleFilters {
  query: string;
  statuses: ArticleStatus[];
  articleTypeIds: string[];
}

/**
 * Filter modal state interface
 */
interface FilterModalState {
  visible: boolean;
}

/**
 * Options for useArticleFilter composable
 */
export interface UseArticleFilterOptions {
  /**
   * Function to load articles with current filters
   */
  loadArticles: () => Promise<void>;

  /**
   * Optional ref to the search input element for auto-focus
   */
  searchInputRef?: Ref<HTMLInputElement | null>;
}

/**
 * Return type for useArticleFilter composable
 */
interface UseArticleFilterReturn {
  filterModal: Ref<FilterModalState>;
  filters: Ref<ArticleFilters>;
  hasActiveFilters: ComputedRef<boolean>;
  activeFilterCount: ComputedRef<number>;
  showFilterModal: () => void;
  closeFilterModal: () => void;
  applyFilters: () => Promise<void>;
  clearFilters: () => Promise<void>;
  toggleStatusFilter: (status: ArticleStatus) => void;
  isStatusSelected: (status: ArticleStatus) => boolean;
  toggleArticleTypeFilter: (typeId: string) => void;
  isArticleTypeSelected: (typeId: string) => boolean;
  buildFilterQueryString: () => string;
  getStatusLabel: (status: ArticleStatus) => string;
  getArticleTypeIconClass: (icon: string | null | undefined) => string;
}

/**
 * Composable for article filtering logic.
 * Manages filter state, modal visibility, and filter operations.
 * 
 * @param options - Configuration options for article filtering
 * @returns Filter state and control methods
 */
export function useArticleFilter(options: UseArticleFilterOptions): UseArticleFilterReturn {
  // Filter modal state
  const filterModal = ref<FilterModalState>({
    visible: false
  });

  // Filter values
  const filters = ref<ArticleFilters>({
    query: '',
    statuses: [],
    articleTypeIds: []
  });

  /**
   * Check if any filters are currently active
   */
  const hasActiveFilters = computed(() => {
    return filters.value.query.trim() !== '' ||
           filters.value.statuses.length > 0 ||
           filters.value.articleTypeIds.length > 0;
  });

  /**
   * Count of active filters for badge display
   */
  const activeFilterCount = computed(() => {
    let count = 0;
    if (filters.value.query.trim() !== '') count++;
    if (filters.value.statuses.length > 0) count++;
    if (filters.value.articleTypeIds.length > 0) count++;
    return count;
  });

  /**
   * Show the filter modal
   */
  const showFilterModal = (): void => {
    filterModal.value.visible = true;
    // Focus on search input after modal is shown
    nextTick(() => {
      if (options.searchInputRef?.value) {
        options.searchInputRef.value.focus();
      }
    });
  };

  /**
   * Close the filter modal
   */
  const closeFilterModal = (): void => {
    filterModal.value.visible = false;
  };

  /**
   * Apply the current filters and reload articles
   */
  const applyFilters = async (): Promise<void> => {
    closeFilterModal();
    await options.loadArticles();
  };

  /**
   * Clear all filters and reload articles
   */
  const clearFilters = async (): Promise<void> => {
    filters.value.query = '';
    filters.value.statuses = [];
    filters.value.articleTypeIds = [];
    closeFilterModal();
    await options.loadArticles();
  };

  /**
   * Toggle a status in the filter
   * @param status - ArticleStatus enum value
   */
  const toggleStatusFilter = (status: ArticleStatus): void => {
    const index = filters.value.statuses.indexOf(status);
    if (index > -1) {
      filters.value.statuses.splice(index, 1);
    } else {
      filters.value.statuses.push(status);
    }
  };

  /**
   * Check if a status is selected
   * @param status - ArticleStatus enum value
   * @returns True if status is selected
   */
  const isStatusSelected = (status: ArticleStatus): boolean => {
    return filters.value.statuses.includes(status);
  };

  /**
   * Toggle an article type in the filter
   * @param typeId - Article type ID
   */
  const toggleArticleTypeFilter = (typeId: string): void => {
    const index = filters.value.articleTypeIds.indexOf(typeId);
    if (index > -1) {
      filters.value.articleTypeIds.splice(index, 1);
    } else {
      filters.value.articleTypeIds.push(typeId);
    }
  };

  /**
   * Check if an article type is selected
   * @param typeId - Article type ID
   * @returns True if article type is selected
   */
  const isArticleTypeSelected = (typeId: string): boolean => {
    return filters.value.articleTypeIds.includes(typeId);
  };

  /**
   * Build query string from current filter state
   * @returns Query string with filters
   */
  const buildFilterQueryString = (): string => {
    const params = new URLSearchParams();

    if (filters.value.query.trim() !== '') {
      params.append('query', filters.value.query.trim());
    }

    filters.value.statuses.forEach(status => {
      params.append('statuses', status.toString());
    });

    filters.value.articleTypeIds.forEach(typeId => {
      params.append('articleTypeIds', typeId);
    });

    const queryString = params.toString();
    return queryString ? `?${queryString}` : '';
  };

  /**
   * Get status label for display
   * @param status - ArticleStatus enum value
   * @returns Human-readable status label
   */
  const getStatusLabel = (status: ArticleStatus): string => {
    const labels: Record<ArticleStatus, string> = {
      [ArticleStatus.Draft]: 'Draft',
      [ArticleStatus.Review]: 'Review',
      [ArticleStatus.Approved]: 'Approved',
      [ArticleStatus.Archived]: 'Archived'
    };
    return labels[status] || 'Unknown';
  };

  /**
   * Get icon class for article type, handling bi- and fa- prefixes
   * @param icon - Icon string from article type
   * @returns Full icon class string
   */
  const getArticleTypeIconClass = (icon: string | null | undefined): string => {
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
  };

  return {
    filterModal,
    filters,
    hasActiveFilters,
    activeFilterCount,
    showFilterModal,
    closeFilterModal,
    applyFilters,
    clearFilters,
    toggleStatusFilter,
    isStatusSelected,
    toggleArticleTypeFilter,
    isArticleTypeSelected,
    buildFilterQueryString,
    getStatusLabel,
    getArticleTypeIconClass
  };
}
