import { ref, computed, type Ref, type ComputedRef } from 'vue';
import type { FragmentCategoryDto } from '@/types/api-client';
import { aiPromptsClient } from '@/utils/apiClients';

export interface UseFragmentCategoriesReturn {
  categories: Ref<FragmentCategoryDto[]>;
  categoryIconMap: ComputedRef<Record<string, string>>;
  categoryIndexMap: ComputedRef<Record<string, FragmentCategoryDto>>;
  loadFragmentCategories: () => Promise<void>;
  loading: Ref<boolean>;
  error: Ref<string | null>;
}

// Shared state across all instances
const sharedCategories = ref<FragmentCategoryDto[]>([]);
const sharedLoading = ref(false);
const sharedError = ref<string | null>(null);
let loadPromise: Promise<void> | null = null;

export function useFragmentCategories(): UseFragmentCategoriesReturn {
  const categoryIconMap = computed(() => {
    const map: Record<string, string> = {};
    sharedCategories.value.forEach(category => {
      if (category.id) {
        map[category.id] = category.icon || 'bi-puzzle';
      }
    });
    return map;
  });

  const categoryIndexMap = computed(() => {
    const map: Record<string, FragmentCategoryDto> = {};
    sharedCategories.value.forEach(category => {
      if (category.id) {
        map[category.id] = category;
      }
    });
    return map;
  });

  const loadFragmentCategories = async (): Promise<void> => {
    if (sharedCategories.value.length > 0) {
      return;
    }

    if (loadPromise) {
      return loadPromise;
    }

    sharedLoading.value = true;
    sharedError.value = null;

    loadPromise = (async () => {
      try {
        sharedCategories.value = await aiPromptsClient.getFragmentCategories();
      } catch (err: any) {
        sharedError.value = 'Failed to load fragment categories: ' + err.message;
        console.error('Error loading fragment categories:', err);
        throw err;
      } finally {
        sharedLoading.value = false;
        loadPromise = null;
      }
    })();

    return loadPromise;
  };

  return {
    categories: sharedCategories,
    categoryIconMap,
    categoryIndexMap,
    loadFragmentCategories,
    loading: sharedLoading,
    error: sharedError
  };
}
