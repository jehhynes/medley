import { ref, computed, nextTick, type Ref, type ComputedRef } from 'vue';
import type { ArticleStatus } from '@/types/api-client';
import { getIconClass } from '@/utils/helpers';

export interface ArticleFilters {
  query: string;
  statuses: ArticleStatus[];
  articleTypeIds: string[];
}

interface FilterModalState {
  visible: boolean;
}

export interface UseArticleFilterOptions {
  loadArticles: () => Promise<void>;
  searchInputRef?: Ref<HTMLInputElement | null>;
}

/** Manages article filtering logic with modal state and filter operations */
export function useArticleFilter(options: UseArticleFilterOptions) {
  const filterModal = ref<FilterModalState>({ visible: false });

  const filters = ref<ArticleFilters>({
    query: '',
    statuses: [],
    articleTypeIds: []
  });

  const hasActiveFilters = computed(() => {
    return filters.value.query.trim() !== '' ||
           filters.value.statuses.length > 0 ||
           filters.value.articleTypeIds.length > 0;
  });

  const activeFilterCount = computed(() => {
    let count = 0;
    if (filters.value.query.trim() !== '') count++;
    if (filters.value.statuses.length > 0) count++;
    if (filters.value.articleTypeIds.length > 0) count++;
    return count;
  });

  const showFilterModal = () => {
    filterModal.value.visible = true;
    nextTick(() => {
      if (options.searchInputRef?.value) {
        options.searchInputRef.value.focus();
      }
    });
  };

  const closeFilterModal = () => {
    filterModal.value.visible = false;
  };

  const applyFilters = async () => {
    closeFilterModal();
    await options.loadArticles();
  };

  const clearFilters = async () => {
    filters.value.query = '';
    filters.value.statuses = [];
    filters.value.articleTypeIds = [];
    closeFilterModal();
    await options.loadArticles();
  };

  const toggleStatusFilter = (status: ArticleStatus) => {
    const index = filters.value.statuses.indexOf(status);
    if (index > -1) {
      filters.value.statuses.splice(index, 1);
    } else {
      filters.value.statuses.push(status);
    }
  };

  const isStatusSelected = (status: ArticleStatus) => {
    return filters.value.statuses.includes(status);
  };

  const toggleArticleTypeFilter = (typeId: string) => {
    const index = filters.value.articleTypeIds.indexOf(typeId);
    if (index > -1) {
      filters.value.articleTypeIds.splice(index, 1);
    } else {
      filters.value.articleTypeIds.push(typeId);
    }
  };

  const isArticleTypeSelected = (typeId: string) => {
    return filters.value.articleTypeIds.includes(typeId);
  };

  const buildFilterQueryString = () => {
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

  const getStatusLabel = (status: ArticleStatus) => {
    const labels: Record<ArticleStatus, string> = {
      [ArticleStatus.Draft]: 'Draft',
      [ArticleStatus.Review]: 'Review',
      [ArticleStatus.Approved]: 'Approved',
      [ArticleStatus.Archived]: 'Archived'
    };
    return labels[status] || 'Unknown';
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
    getArticleTypeIconClass: getIconClass
  };
}
