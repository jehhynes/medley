import { ref, computed, type Ref, type ComputedRef } from 'vue';
import type { ArticleTypeDto } from '@/types/api-client';
import { getArticleTypes } from '@/utils/helpers';

export interface UseArticleTypesReturn {
  types: Ref<ArticleTypeDto[]>;
  typeIconMap: ComputedRef<Record<string, string>>;
  typeIndexMap: ComputedRef<Record<string, ArticleTypeDto>>;
  loadArticleTypes: () => Promise<void>;
  loading: Ref<boolean>;
  error: Ref<string | null>;
}

// Shared state across all instances
const sharedTypes = ref<ArticleTypeDto[]>([]);
const sharedLoading = ref(false);
const sharedError = ref<string | null>(null);
let loadPromise: Promise<void> | null = null;

export function useArticleTypes(): UseArticleTypesReturn {
  const typeIconMap = computed(() => {
    const map: Record<string, string> = {};
    sharedTypes.value.forEach(type => {
      if (type.id) {
        map[type.id] = type.icon || 'bi-file-text';
      }
    });
    return map;
  });

  const typeIndexMap = computed(() => {
    const map: Record<string, ArticleTypeDto> = {};
    sharedTypes.value.forEach(type => {
      if (type.id) {
        map[type.id] = type;
      }
    });
    return map;
  });

  const loadArticleTypes = async (): Promise<void> => {
    if (sharedTypes.value.length > 0) {
      return;
    }

    if (loadPromise) {
      return loadPromise;
    }

    sharedLoading.value = true;
    sharedError.value = null;

    loadPromise = (async () => {
      try {
        sharedTypes.value = await getArticleTypes();
      } catch (err: any) {
        sharedError.value = 'Failed to load article types: ' + err.message;
        console.error('Error loading article types:', err);
        throw err;
      } finally {
        sharedLoading.value = false;
        loadPromise = null;
      }
    })();

    return loadPromise;
  };

  return {
    types: sharedTypes,
    typeIconMap,
    typeIndexMap,
    loadArticleTypes,
    loading: sharedLoading,
    error: sharedError
  };
}
