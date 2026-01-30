import { ref, computed, type Ref, type ComputedRef } from 'vue';
import type { KnowledgeCategoryDto } from '@/types/api-client';
import { aiPromptsClient } from '@/utils/apiClients';

export interface UseKnowledgeCategoriesReturn {
  categories: Ref<KnowledgeCategoryDto[]>;
  categoryIconMap: ComputedRef<Record<string, string>>;
  categoryIndexMap: ComputedRef<Record<string, KnowledgeCategoryDto>>;
  loadKnowledgeCategories: () => Promise<void>;
  loading: Ref<boolean>;
  error: Ref<string | null>;
}

// Shared state across all instances
const sharedCategories = ref<KnowledgeCategoryDto[]>([]);
const sharedLoading = ref(false);
const sharedError = ref<string | null>(null);
let loadPromise: Promise<void> | null = null;

export function useKnowledgeCategories(): UseKnowledgeCategoriesReturn {
  const categoryIconMap = computed(() => {
    const map: Record<string, string> = {};
    sharedCategories.value.forEach(category => {
      if (category.id) {
        map[category.id] = category.icon || 'fa-light fa-atom';
      }
    });
    return map;
  });

  const categoryIndexMap = computed(() => {
    const map: Record<string, KnowledgeCategoryDto> = {};
    sharedCategories.value.forEach(category => {
      if (category.id) {
        map[category.id] = category;
      }
    });
    return map;
  });

  const loadKnowledgeCategories = async (): Promise<void> => {
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
        sharedCategories.value = await aiPromptsClient.getKnowledgeCategories();
      } catch (err: any) {
        sharedError.value = 'Failed to load knowledge categories: ' + err.message;
        console.error('Error loading knowledge categories:', err);
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
    loadKnowledgeCategories,
    loading: sharedLoading,
    error: sharedError
  };
}
