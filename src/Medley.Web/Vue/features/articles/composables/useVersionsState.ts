import { ref, computed, type Ref, type ComputedRef } from 'vue';
import type { ArticleVersionDto, VersionType } from '@/types/api-client';

/**
 * State for article versions
 */
interface VersionsState {
  versions: Ref<ArticleVersionDto[]>;
  loading: Ref<boolean>;
  error: Ref<string | null>;
  loaded: Ref<boolean>;
}

/**
 * Return type for useVersionsState composable
 */
interface UseVersionsStateReturn {
  versions: ComputedRef<ArticleVersionDto[]>;
  userVersions: ComputedRef<ArticleVersionDto[]>;
  pendingAiVersion: ComputedRef<ArticleVersionDto | undefined>;
  getVersionById: (versionId: string) => ArticleVersionDto | undefined;
  loading: ComputedRef<boolean>;
  error: ComputedRef<string | null>;
  loaded: ComputedRef<boolean>;
  setVersions: (newVersions: ArticleVersionDto[]) => void;
  setLoading: (isLoading: boolean) => void;
  setError: (err: string | null) => void;
  clearCache: (id: string) => void;
}

// Cache for versions state per article
const versionsCache = new Map<string, VersionsState>();

/**
 * Composable for managing article versions with caching.
 * Provides reactive state for versions, loading, and error states.
 * 
 * @param articleId - Reactive reference to the article ID
 * @returns Versions state and management methods
 */
export function useVersionsState(articleId: Ref<string | null | undefined>): UseVersionsStateReturn {
  /**
   * Get or create versions state for an article
   */
  const getOrCreateVersionsState = (id: string): VersionsState => {
    if (!versionsCache.has(id)) {
      versionsCache.set(id, {
        versions: ref<ArticleVersionDto[]>([]),
        loading: ref<boolean>(false),
        error: ref<string | null>(null),
        loaded: ref<boolean>(false)
      });
    }
    return versionsCache.get(id)!;
  };

  const state = computed(() => 
    articleId?.value ? getOrCreateVersionsState(articleId.value) : null
  );

  const versions = computed(() => state.value?.versions.value || []);
  const loading = computed(() => state.value?.loading.value ?? false);
  const error = computed(() => state.value?.error.value ?? null);
  const loaded = computed(() => state.value?.loaded.value ?? false);

  /**
   * Get only user-created versions (excludes AI versions)
   */
  const userVersions = computed(() => 
    versions.value.filter(v => v.versionType === 'User' as VersionType)
  );

  /**
   * Get the pending AI version if one exists
   */
  const pendingAiVersion = computed(() => 
    versions.value.find(v => v.versionType === 'AI' as VersionType)
  );

  /**
   * Get a specific version by ID
   */
  const getVersionById = (versionId: string): ArticleVersionDto | undefined => {
    return versions.value.find(v => v.id === versionId);
  };

  /**
   * Set versions for the current article
   */
  const setVersions = (newVersions: ArticleVersionDto[]): void => {
    if (state.value) {
      state.value.versions.value = newVersions;
      state.value.loaded.value = true;
    }
  };

  /**
   * Set loading state
   */
  const setLoading = (isLoading: boolean): void => {
    if (state.value) {
      state.value.loading.value = isLoading;
    }
  };

  /**
   * Set error state
   */
  const setError = (err: string | null): void => {
    if (state.value) {
      state.value.error.value = err;
    }
  };

  /**
   * Clear cached versions for a specific article
   */
  const clearCache = (id: string): void => {
    versionsCache.delete(id);
  };

  return {
    versions,
    userVersions,
    pendingAiVersion,
    getVersionById,
    loading,
    error,
    loaded,
    setVersions,
    setLoading,
    setError,
    clearCache
  };
}
